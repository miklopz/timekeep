using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Http;
using TimeKeep.Web.API.Data;

namespace TimeKeep.Web.API.Models
{
    public sealed class Error
    {
        public Guid ID { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public int? StatusCode { get; set; }
        public int? PID { get; set; }
        public int? TID { get; set; }

        public void Create()
        {
            if (string.IsNullOrEmpty(Type) || Type.Trim().Length == 0)
                throw new ArgumentNullException("Type");
            if (string.IsNullOrEmpty(Message) || Message.Trim().Length == 0)
                throw new ArgumentNullException("Message");

            DataAccess dal = new DataAccess("Errors");
            Error error = null;
            using (IDataReader reader = dal.Create(
                new Dictionary<string, object>
                {
                    ["@Type"] = Type,
                    ["@Message"] = Message,
                    ["@StackTrace"] = StackTrace == null ? DBNull.Value : (object)StackTrace,
                    ["@StatusCode"] = StatusCode.HasValue ? (object)StatusCode.Value : DBNull.Value,
                    ["@PID"] = PID.HasValue ? (object)PID.Value : DBNull.Value,
                    ["@TID"] = TID.HasValue ? (object)TID.Value : DBNull.Value
                }
            ))
            {
                error = ReadOneFromDataReader(reader);
            }

            this.ID = error.ID;
            this.TimeStamp = error.TimeStamp;
            this.Message = error.Message;
            this.StackTrace = error.StackTrace;
            this.PID = error.PID;
            this.TID = error.TID;
        }

        private static Error ReadOneFromDataReader(IDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (reader.IsClosed)
                throw new ArgumentException("reader is closed");

            if (!reader.Read())
                return null;
            return Parse(reader);
        }

        private static Error Parse(IDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (reader.IsClosed)
                throw new ArgumentException("reader is closed");

            return new Error
            {
                ID = (Guid)reader["RowID"],
                TimeStamp = (DateTime)reader["TimeStamp"],
                Type = (string)reader["Type"],
                Message = (string)reader["Message"],
                StackTrace = reader["StackTrace"] == DBNull.Value ? null : (string)reader["StackTrace"],
                StatusCode = reader["StatusCode"] == DBNull.Value ? (int?)null : (int)reader["StatusCode"],
                PID = reader["PID"] == DBNull.Value ? (int?)null : (int)reader["PID"],
                TID = reader["TID"] == DBNull.Value ? (int?)null : (int)reader["TID"]
            };
        }

        public static Error Log(string type, string message, string stackTrace = null, int? statusCode = null, int? pid = null, int? tid = null)
        {
            Error err = new Error()
            {
                Type = type,
                Message = message,
                StackTrace = stackTrace,
                StatusCode = statusCode,
                PID = pid,
                TID = tid
            };

            try
            {
                err.Create();
            }
            catch (Exception ex)
            {
                try
                {
                    if (!EventLog.SourceExists("TimeKeep.Web.API"))
                    {
                        EventLog.CreateEventSource("TimeKeep.Web.API", "Application");
                        EventLog.WriteEntry("TimeKeep.Web.API", "Created new log \"Application\"", EventLogEntryType.Information);
                    }

                    EventLog.WriteEntry("TimeKeep.Web.API", string.Format("Could not write log. Error Message: [{0}]{1}\r\nStack Trace:\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace), EventLogEntryType.Error);
                }
                catch
                {
                    // Wow, at least allow a debugger to find out what went wrong
                    Debug.Write(string.Format("[DEBUG] Could not write log. Error Message: [{0}]{1}\r\nStack Trace:\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace));
                }
                err.ID = Guid.Empty;
                err.TimeStamp = DateTime.UtcNow;
            }

            return err;
        }

        public static Error Log(string type, string message, string stackTrace = null, int? statusCode = null)
        {
            int? pid = null;
            int? tid = null;
            try
            {
                pid = Process.GetCurrentProcess().Id;
            }
            catch
            {

            }

            tid = Thread.CurrentThread.ManagedThreadId;

            return Log(type, message, stackTrace, statusCode, pid, tid);
        }

        public static Error Log(Exception ex, int? statusCode = null)
        {
            // Please don't pass null, honestly. You'll regret it :).
            if (ex == null)
                throw new ArgumentNullException("This overload requires that ex is not null", "ex");
            return Log(ex.GetType().ToString(), ex.Message, ex.StackTrace, statusCode);
        }

        public static Error Log(HttpResponseException ex)
        {
            int? substatusCode = null;
            int temp = 0;
            if(ex.Response.ReasonPhrase != null && ex.Response.ReasonPhrase.Length > 0 && int.TryParse(ex.Response.ReasonPhrase, out temp))
            {
                substatusCode = temp;
            }

            return Log(ex.InnerException, (int)ex.Response.StatusCode);
        }
    }
}