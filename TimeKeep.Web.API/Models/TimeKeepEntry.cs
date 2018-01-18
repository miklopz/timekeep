using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using TimeKeep.Web.API.Data;

namespace TimeKeep.Web.API.Models
{
    public sealed class TimeKeepEntry
    {
        public Guid? ID { get; set; }
        public string User { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public Category Category { get; set; }
        public string CaseNumber { get; set; }
        public bool IsLogged { get; set; }
        public bool IsDetailed { get; set; }

        /// <summary>
        /// String representation of the time keep entry's labor
        /// </summary>
        public string Labor
        {
            get
            {
                if (!EndTime.HasValue)
                    return null;

                // ML: Bugfix. Originally, the minutes were computed then rounded up to the next minute of seconds or milliseconds happened later.
                // But diff was computed without the round up and the string append used minutes for minutes and diff.Hours for hours.
                // This resulted in 0:60 being displayed instead of 1:00 for times that rounded up to an exact hour

                TimeSpan delta = (EndTime.Value - StartTime);
                double minutes = (EndTime.Value - StartTime).TotalMinutes + ((delta.Seconds + delta.Milliseconds > 0) ? 1 : 0);
                TimeSpan diff = TimeSpan.FromMinutes(minutes);
                
                diff = TimeSpan.FromMinutes(minutes);

                StringBuilder sb = new StringBuilder();
                if(diff.Days > 0)
                {
                    sb.Append(diff.Days);
                    sb.Append(".");
                }
                sb.Append(diff.Hours);
                sb.Append(":");
                if (diff.Minutes < 10)
                    sb.Append("0");
                sb.Append(diff.Minutes);

                return sb.ToString();
            }
        }

        internal TimeSpan? LaborTS
        {
            get
            {
                if (!EndTime.HasValue)
                    return null;

                TimeSpan diff = TimeSpan.FromMinutes((EndTime.Value - StartTime).TotalMinutes);

                int minutes = diff.Minutes + (diff.Seconds + diff.Milliseconds > 0 ? 1 : 0);

                return new TimeSpan(diff.Days, diff.Hours, minutes, 0);
            }
        }

        private static TimeKeepEntry Parse(IDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (reader.IsClosed)
                throw new ArgumentException("reader is closed");

            return new TimeKeepEntry
            {
                ID = (Guid)reader["RowID"],
                User = (string)reader["User"],
                StartTime = (DateTime)reader["StartTime"],
                EndTime = reader["EndTime"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["EndTime"],
                Category = reader["Category"] == DBNull.Value ? null : Category.ReadByID((Guid)reader["Category"]),
                CaseNumber = reader["CaseNumber"] == DBNull.Value ? null : (string)reader["CaseNumber"],
                IsLogged = (bool)reader["IsLogged"],
                IsDetailed = (bool)reader["IsDetailed"]
            };
        }

        private static TimeKeepEntry ReadOneFromDataReader(IDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (reader.IsClosed)
                throw new ArgumentException("reader is closed");

            if (!reader.Read())
                return null;
            return Parse(reader);
        }

        private static IEnumerable<TimeKeepEntry> ReadListFromDataReader(IDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (reader.IsClosed)
                throw new ArgumentException("reader is closed");

            IList<TimeKeepEntry> list = new List<TimeKeepEntry>();

            while (reader.Read())
                list.Add(Parse(reader));
            return list;
        }

        public TimeKeepEntry Create()
        {
            // Validate
            if (string.IsNullOrEmpty(User) || string.IsNullOrWhiteSpace(User))
                throw new ArgumentNullException("user");

            if (ID.HasValue)
            {
                if (Category == null)
                    throw new ArgumentNullException("category");

                if (Category.IsScorecard && (string.IsNullOrEmpty(CaseNumber) || string.IsNullOrWhiteSpace(CaseNumber)))
                    throw new ArgumentNullException("category", "Parameter cannot be null if the labor is a scorecard labor");

                if (!Category.IsScorecard && !string.IsNullOrEmpty(CaseNumber) && !string.IsNullOrEmpty(CaseNumber))
                    throw new ArgumentException("Cannot specify a case number for non-scorecard labor");
            }


            DataAccess dal = new DataAccess("TimeKeepEntries");
            // This is supposed to return the altered and the new row
            TimeKeepEntry newEntry = null, modifiedEntry = null;
            using (IDataReader reader = dal.Create(
                new Dictionary<string, object>
                {
                    ["@RowID"] = (ID.HasValue ? (object)ID.Value : DBNull.Value),
                    ["@User"] = User.Trim(),
                    ["@Category"] = (Category == null ? DBNull.Value : (object)Category.ID),
                    ["@CaseNumber"] = (CaseNumber == null ? DBNull.Value : (object)CaseNumber.Trim())
                }
            ))
            {
                newEntry = ReadOneFromDataReader(reader);
                modifiedEntry = ReadOneFromDataReader(reader);
            }

            this.ID = newEntry.ID;
            this.User = newEntry.User;
            this.IsDetailed = newEntry.IsDetailed;
            this.IsLogged = newEntry.IsLogged;
            this.StartTime = newEntry.StartTime;
            this.CaseNumber = newEntry.CaseNumber;
            this.Category = newEntry.Category;
            this.EndTime = newEntry.EndTime;

            return modifiedEntry;
        }

        public static IEnumerable<TimeKeepEntry> ReadByUser(string user, DateRange range)
        {
            if (user == null || string.IsNullOrEmpty(user) || string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException("user");
            if (range == null)
                throw new ArgumentNullException("range");
            DataAccess dal = new DataAccess("TimeKeepEntries");
            using (IDataReader reader = dal.ReadFilter("ByUser", new Dictionary<string, object>
            {
                ["@User"] = user,
                ["@StartDate"] = range.Start,
                ["@EndDate"] = range.End,
            }))
            {
                return ReadListFromDataReader(reader);
            }
        }

        /// <summary>
        /// Gets a list of time keep entries given a case number and range
        /// </summary>
        /// <param name="casenumber">Case Number to search</param>
        /// <param name="user">The user making the call</param>
        /// <param name="range">A date range</param>
        /// <returns>IEnumerable&lt;TimeKeepEntri&gt; representing the results</returns>
        public static IEnumerable<TimeKeepEntry> ReadByCase(string casenumber, string user, DateRange range)
        {
            if (casenumber == null || string.IsNullOrEmpty(casenumber) || string.IsNullOrWhiteSpace(casenumber))
                throw new ArgumentNullException("casenumber");
            if (user == null || string.IsNullOrEmpty(user) || string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException("user");
            if (range == null)
                throw new ArgumentNullException("range");

            DataAccess dal = new DataAccess("TimeKeepEntries");
            using (IDataReader reader = dal.ReadFilter("ByCase", new Dictionary<string, object>
            {
                ["@CaseNumber"] = casenumber,
                ["@User"] = user,
                ["@StartTime"] = range.Start,
                ["@EndTime"] = range.End,
            }))
            {
                return ReadListFromDataReader(reader);
            }
        }

        public static IEnumerable<TimeKeepEntry> LogAndDetailAllByCase(string casenumber, string user, DateRange range)
        {
            if (casenumber == null || string.IsNullOrEmpty(casenumber) || string.IsNullOrWhiteSpace(casenumber))
                throw new ArgumentNullException("casenumber");
            if (user == null || string.IsNullOrEmpty(user) || string.IsNullOrWhiteSpace(user))
                throw new ArgumentNullException("user");
            if (range == null)
                throw new ArgumentNullException("range");

            DataAccess dal = new DataAccess("TimeKeepEntries");
            using (IDataReader reader = dal.UpdateFilter("IsLoggedAndDetailedForAllCase", new Dictionary<string, object>
            {
                ["@CaseNumber"] = casenumber,
                ["@User"] = user,
                ["@StartTime"] = range.Start,
                ["@EndTime"] = range.End,
            }))
            {
                return ReadListFromDataReader(reader);
            }
        }

        public static TimeKeepEntry Read(Guid id)
        {
            DataAccess dal = new DataAccess("TimeKeepEntries");
            using (IDataReader reader = dal.Read(new Dictionary<string, object>
            {
                ["@RowID"] = id
            }))
            {
                return ReadOneFromDataReader(reader);
            }
        }

        public void Update()
        {
            // Validate
            if (string.IsNullOrEmpty(User) || string.IsNullOrWhiteSpace(User))
                throw new ArgumentNullException("user");

            if (Category == null)
                throw new ArgumentNullException("category");

            if (Category.IsScorecard && (string.IsNullOrEmpty(CaseNumber) || string.IsNullOrWhiteSpace(CaseNumber)))
                throw new ArgumentNullException("category", "Parameter cannot be null if the labor is a scorecard labor");

            if (!Category.IsScorecard && !string.IsNullOrEmpty(CaseNumber) && !string.IsNullOrEmpty(CaseNumber))
                throw new ArgumentException("Cannot specify a case number for non-scorecard labor");

            if (EndTime.HasValue && EndTime <= StartTime)
                throw new ArgumentOutOfRangeException("EndTime", "The end time must be after the start time");

            DataAccess dal = new DataAccess("TimeKeepEntries");
            TimeKeepEntry modifiedItem = null;
            using (IDataReader reader = dal.Update(
                new Dictionary<string, object>
                {
                    ["@RowID"] = this.ID,
                    ["@StartTime"] = this.StartTime,
                    ["@EndTime"] = (object)this.EndTime ?? DBNull.Value,
                    ["@Category"] = this.Category == null ? DBNull.Value : (object)this.Category.ID,
                    ["@CaseNumber"] = (string.IsNullOrEmpty(CaseNumber) || string.IsNullOrWhiteSpace(CaseNumber)) ? DBNull.Value : (object)this.CaseNumber
                }
            ))
            {
                modifiedItem = ReadOneFromDataReader(reader);
            }

            this.ID = modifiedItem.ID;
            this.CaseNumber = modifiedItem.CaseNumber;
            this.Category = modifiedItem.Category;
            this.EndTime = modifiedItem.EndTime;
            this.IsDetailed = modifiedItem.IsDetailed;
            this.IsLogged = modifiedItem.IsLogged;
            this.StartTime = modifiedItem.StartTime;
            this.User = modifiedItem.User;
        }

        public void ToggleIsDetailed()
        {
            DataAccess dal = new DataAccess("TimeKeepEntries");
            TimeKeepEntry modifiedItem = null;
            using (IDataReader reader = dal.UpdateFilter("IsDetailed", new Dictionary<string, object> { ["@RowID"] = this.ID }))
            {
                modifiedItem = ReadOneFromDataReader(reader);
            }

            this.ID = modifiedItem.ID;
            this.CaseNumber = modifiedItem.CaseNumber;
            this.Category = modifiedItem.Category;
            this.EndTime = modifiedItem.EndTime;
            this.IsDetailed = modifiedItem.IsDetailed;
            this.IsLogged = modifiedItem.IsLogged;
            this.StartTime = modifiedItem.StartTime;
            this.User = modifiedItem.User;
        }

        public void ToggleIsLogged()
        {
            DataAccess dal = new DataAccess("TimeKeepEntries");
            TimeKeepEntry modifiedItem = null;
            using (IDataReader reader = dal.UpdateFilter("IsLogged", new Dictionary<string, object> { ["@RowID"] = this.ID }))
            {
                modifiedItem = ReadOneFromDataReader(reader);
            }

            this.ID = modifiedItem.ID;
            this.CaseNumber = modifiedItem.CaseNumber;
            this.Category = modifiedItem.Category;
            this.EndTime = modifiedItem.EndTime;
            this.IsDetailed = modifiedItem.IsDetailed;
            this.IsLogged = modifiedItem.IsLogged;
            this.StartTime = modifiedItem.StartTime;
            this.User = modifiedItem.User;
        }

        public void Delete()
        {
            DataAccess dal = new DataAccess("TimeKeepEntries");
            int affectedRows = dal.Delete(new Dictionary<string, object> { ["@RowID"] = this.ID });
            if (affectedRows == 0 || affectedRows > 1)
                throw new InvalidOperationException("Delete failed, no rows or multiple rows affected");
        }
    }
}