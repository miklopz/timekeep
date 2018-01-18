using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace TimeKeep.Web.UI
{
    public sealed class BroadcastHandler : IHttpHandler
    {
        private sealed class BroadcastClient
        {
            public Guid ID { get; set; }
            public string User { get; set; }
            public Queue<Change> Changes {get;set;}

            public BroadcastClient()
            {

            }
            public BroadcastClient(string user)
            {
                ID = Guid.NewGuid();
                User = user;
                Changes = new Queue<BroadcastHandler.Change>();
            }

            private static ReaderWriterLock opLock = new ReaderWriterLock();
            private static Dictionary<string, object> chLock = new Dictionary<string, object>();

            private static List<BroadcastClient> _clients;
            public static List<BroadcastClient> Clients
            {
                get
                {
                    if(_clients == null)
                    {
                        lock(opLock)
                        {
                            if(_clients == null)
                            {
                                _clients = new List<BroadcastClient>();
                            }
                        }
                    }
                    return _clients;
                }
            }

            public static Guid Register(string user)
            {
                opLock.AcquireWriterLock(-1);
                try
                {
                    BroadcastClient newbc = new BroadcastClient(user);
                    Clients.Add(newbc);
                    if (!chLock.ContainsKey(user))
                        chLock.Add(user, new object());
                    return newbc.ID;
                }
                finally
                {
                    opLock.ReleaseWriterLock();
                }
            }

            public static void Broadcast(Guid id, Change change)
            {
                opLock.AcquireReaderLock(-1);
                try
                {
                    BroadcastClient submiting = Clients.Where(c => c.ID.Equals(id)).First();
                    lock (chLock[submiting.User])
                    {
                        foreach (BroadcastClient client in Clients)
                        {
                            if (!client.ID.Equals(id) && client.User.Equals(submiting.User))
                            {
                                if (change.changeType.Equals(ChangeType.EndOfDay))
                                    client.Changes.Clear();
                                client.Changes.Enqueue(change);
                            }
                        }
                    }
                }
                finally
                {
                    opLock.ReleaseReaderLock();
                }
            }

            public static IEnumerable<Change> GetChanges(Guid id)
            {
                BroadcastClient client = Clients.Where(c => c.ID.Equals(id)).FirstOrDefault();
                List<Change> changes = new List<Change>(client == null ? 0 : client.Changes.Count);
                if (client == null) return changes;
                lock(chLock[client.User])
                {
                    opLock.AcquireReaderLock(-1);
                    try
                    {
                        while (client.Changes.Count > 0)
                            changes.Add(client.Changes.Dequeue());
                        return changes;
                    }
                    finally
                    {
                        opLock.ReleaseReaderLock();
                    }
                }
            }

            public static void Deregister(Guid id)
            {
                BroadcastClient client = Clients.Where(c => c.ID.Equals(id)).FirstOrDefault();
                if (client == null) return;
                opLock.AcquireWriterLock(-1);
                try
                {
                    if (Clients.Where(c => c.User.Equals(client.User)).Count() == 1)
                        if (chLock.ContainsKey(client.User))
                            chLock.Remove(client.User);
                    Clients.Remove(client);
                }
                finally
                {
                    opLock.ReleaseWriterLock();
                }
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                BroadcastClient val = obj as BroadcastClient;
                if (val == null) return false;
                return this.ID.Equals(val.ID);
            }

            public override int GetHashCode()
            {
                return this.ID.GetHashCode();
            }

            public override string ToString()
            {
                return string.Concat("Client ID: ", this.ID, ", User: ", this.User);
            }
        }

        private sealed class Category
        {
            public Guid ID { get; set; }
            public string Description { get; set; }
            public bool IsScorecard { get; set; }
            public bool IsOut { get; set; }
        }

        private sealed class TimeKeepEntry
        {
            public Guid ID { get; set; }
            public string User { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public Category Category { get; set; }
            public string CaseNumber { get; set; }
            public bool IsLogged { get; set; }
            public bool IsDetailed { get; set; }

            public string Labor
            {
                get
                {
                    if (!EndTime.HasValue)
                        return null;

                    TimeSpan diff = TimeSpan.FromMinutes((EndTime.Value - StartTime).TotalMinutes);

                    int minutes = diff.Minutes + (diff.Seconds > 0 ? 1 : 0);

                    StringBuilder sb = new StringBuilder();
                    if (diff.Days > 0)
                    {
                        sb.Append(diff.Days);
                        sb.Append(".");
                    }
                    sb.Append(diff.Hours);
                    sb.Append(":");
                    if (minutes < 10)
                        sb.Append("0");
                    sb.Append(minutes);

                    return sb.ToString();
                }
            }
        }

        private sealed class BroadcastInput
        {
            public Guid ID { get; set; }
            public Change Change { get; set; }
        }

        private sealed class Change
        {
            public string changeType;
            public TimeKeepEntry data;

            public Change(string changeType, TimeKeepEntry data)
            {
                this.changeType = changeType;
                this.data = data;
            }
        }

        private static class ChangeType
        {
            public static readonly string Added = "Added";
            public static readonly string Modified = "Modified";
            public static readonly string Deleted = "Deleted";
            public static readonly string EndOfDay = "EndOfDay";
        }

        private class Error
        {
            public Guid ID { get; set; }
            public DateTime TimeStamp { get; set; }
            public string Type { get; set; }
            public string Message { get; set; }
            public string StackTrace { get; set; }
            public int? StatusCode { get; set; }
            public int? PID { get; set; }
            public int? TID { get; set; }
        }

        public bool IsReusable{ get { return true; } }

        public int? ProcessID
        {
            get
            {
                try
                {
                    return Process.GetCurrentProcess().Id;
                }
                catch
                {
                    return null;
                }
            }
        }

        public int? ThreadID
        {
            get
            {
                try
                {
                    return Thread.CurrentThread.ManagedThreadId;
                }
                catch
                {
                    return null;
                }
            }
        }

        public Guid NewID
        {
            get { return Guid.NewGuid(); }
        }

        public void ProcessRequest(HttpContext context)
        {
            HttpRequest Request = context.Request;
            HttpResponse Response = context.Response;
            BroadcastClient client = null;
            BroadcastInput input = null;
            Error error = null;

            if(Request.Url.AbsolutePath.EndsWith("/broadcast/register"))
            {
                client = ReadBodyAsBroadcastClient(Request);
                if(client == null || string.IsNullOrEmpty(client.User) || string.IsNullOrWhiteSpace(client.User))
                {
                    Response.StatusCode = 400;
                    Response.ContentType = "application/json";
                    error = new Error();
                    error.ID = NewID;
                    error.TID = ThreadID;
                    error.PID = ProcessID;
                    error.Message = "The User is missing.";
                    error.StatusCode = 400;
                    error.TimeStamp = DateTime.UtcNow;
                    Response.Write(JsonConvert.SerializeObject(error));
                    context.ApplicationInstance.CompleteRequest();
                    return;
                }

                string guid = BroadcastClient.Register(client.User).ToString();
                Response.StatusCode = 201;
                Response.ContentType = "application/json";
                Response.Write("{\"Result\":{\"ID\":\"" + guid + "\"}}");
                context.ApplicationInstance.CompleteRequest();
                return;

            }
            else if(Request.Url.AbsolutePath.EndsWith("/broadcast/broadcast"))
            {
                input = ReadBodyAsBroadcastInput(Request);
                if (input == null || input.Change == null)
                {
                    Response.StatusCode = 400;
                    Response.ContentType = "application/json";
                    error = new Error();
                    error.ID = NewID;
                    error.TID = ThreadID;
                    error.PID = ProcessID;
                    error.Message = "Invalid request body.";
                    error.StatusCode = 400;
                    error.TimeStamp = DateTime.UtcNow;
                    Response.Write(JsonConvert.SerializeObject(error));
                    context.ApplicationInstance.CompleteRequest();
                    return;
                }

                BroadcastClient.Broadcast(input.ID, input.Change);
                Response.StatusCode = 202;
                Response.ContentType = "application/json";
                context.ApplicationInstance.CompleteRequest();
                return;
            }
            else if (Request.Url.AbsolutePath.EndsWith("/broadcast/changes"))
            {
                input = ReadBodyAsBroadcastInput(Request);
                if (input == null)
                {
                    Response.StatusCode = 400;
                    Response.ContentType = "application/json";
                    error = new Error();
                    error.ID = NewID;
                    error.TID = ThreadID;
                    error.PID = ProcessID;
                    error.Message = "Invalid request body.";
                    error.StatusCode = 400;
                    error.TimeStamp = DateTime.UtcNow;
                    Response.Write(JsonConvert.SerializeObject(error));
                    context.ApplicationInstance.CompleteRequest();
                    return;
                }

                string changes = "{\"Result\":" + JsonConvert.SerializeObject(BroadcastClient.GetChanges(input.ID)) + "}";
                Response.StatusCode = 200;
                Response.ContentType = "application/json";
                Response.Write(changes);
                context.ApplicationInstance.CompleteRequest();
                return;
            }
            else if (Request.Url.AbsolutePath.EndsWith("/broadcast/deregister"))
            {
                input = ReadBodyAsBroadcastInput(Request);
                if (input == null)
                {
                    Response.StatusCode = 400;
                    error = new Error();
                    error.ID = NewID;
                    error.TID = ThreadID;
                    error.PID = ProcessID;
                    error.Message = "Invalid request body.";
                    error.StatusCode = 400;
                    error.TimeStamp = DateTime.UtcNow;
                    Response.Write(JsonConvert.SerializeObject(error));
                    context.ApplicationInstance.CompleteRequest();
                    return;
                }

                BroadcastClient.Deregister(input.ID);
                Response.StatusCode = 200;
                Response.ContentType = "application/json";
                context.ApplicationInstance.CompleteRequest();
                return;
            }



            Response.StatusCode = 404;
            Response.ContentType = "application/json";
            error = new Error();
            error.ID = NewID;
            error.TID = ThreadID;
            error.PID = ProcessID;
            error.Message = "Resource not found";
            error.StatusCode = 404;
            error.TimeStamp = DateTime.UtcNow;
            Response.Write(JsonConvert.SerializeObject(error));
            context.ApplicationInstance.CompleteRequest();
            return;
        }

        private BroadcastClient ReadBodyAsBroadcastClient(HttpRequest request)
        {
            try
            {
                using (Stream st = request.GetBufferedInputStream())
                using (StreamReader sr = new StreamReader(st))
                {
                    return JsonConvert.DeserializeObject<BroadcastClient>(sr.ReadToEnd());
                }
            }
            catch
            {
                return null;
            }
        }
        private BroadcastInput ReadBodyAsBroadcastInput(HttpRequest request)
        {
            try
            {
                using (Stream st = request.GetBufferedInputStream())
                using (StreamReader sr = new StreamReader(st))
                {
                    return JsonConvert.DeserializeObject<BroadcastInput>(sr.ReadToEnd());
                }
            }
            catch
            {
                return null;
            }
        }
    }
}