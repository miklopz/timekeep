using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Principal;
using System.Web.Http;
using TimeKeep.Web.API.Models;

namespace TimeKeep.Web.API.Controllers
{
    [Authorize]
    [Route("TimeKeepEntries")]
    public class TimeKeepEntriesController : ApiController
    {
        private static readonly ArgumentNullException userIsNull = new ArgumentNullException("user");
        private static readonly ArgumentNullException idIsNull = new ArgumentNullException("ID");
        private static readonly ArgumentNullException rangeIsNull = new ArgumentNullException("range");
        private static readonly ArgumentNullException CategoryIsNull = new ArgumentNullException("Category");
        private static readonly ArgumentNullException UserIsNull = new ArgumentNullException("User");
        private static readonly ArgumentNullException CaseNumberIsNull = new ArgumentNullException("CaseNumber", "CaseNumber cannot be null if the labor is a scorecard labor.");
        private static readonly ArgumentNullException CaseNumberIsNull2 = new ArgumentNullException("CaseNumber", "The case number cannot be null for this operation.");
        private static readonly ArgumentException CaseNumberIsNotNull = new ArgumentException("CaseNumber", "CaseNumber cannot be null if the labor is a scorecard labor.");
        private static readonly ArgumentException MissingOrInvalidRequestBody = new ArgumentException("Request Body", "The request body is missing or invalid.");
        private static readonly ArgumentException EndDateAtOrBeforeStartDate = new ArgumentException("EndDate", "The EndDate cannot take place at or before the Start Date. You cannot time travel. If you do, please contact support.");
        private static readonly EntriesNotFoundException noEntries = new EntriesNotFoundException("No time keep entries were found");
        private static readonly SecurityException userNotAllowed = new SecurityException("Operation not allowed.");
        private sealed class EntriesNotFoundException : Exception
        {
            public EntriesNotFoundException() : base()
            {

            }

            public EntriesNotFoundException(string message) : base(message)
            {

            }

            public EntriesNotFoundException(string message, Exception innerException) : base(message, innerException)
            {

            }
        }

        private bool ValidateUser(IPrincipal principal, string user)
        {
            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
                return false;
            if (user == null || user.Trim().Length == 0)
                return false;
            return GetPrincipalUser(principal).Equals(user.Replace(" ", string.Empty), StringComparison.InvariantCultureIgnoreCase);
        }

        private bool ValidateUser(IPrincipal principal, TimeKeepEntry entry)
        {
            if (entry == null)
                return false;
            return ValidateUser(principal, entry.User);
        }

        private string GetPrincipalUser(IPrincipal principal)
        {
            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
                return null;
            return principal.Identity.Name.Replace(" ", string.Empty).ToLowerInvariant();
        }



        /// <summary>
        /// Gets the list of time keep entries for a given user and time range
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="range">The range</param>
        /// <returns></returns>
        [HttpPost]
        [Route("TimeKeepEntries/User/{user:alpha}")]
        public HttpResponseMessage GetByUser(string user, [FromBody] DateRange range)
        {
            try
            {
                if(!ValidateUser(RequestContext.Principal, user))
                    return Request.CreateCustomErrorResponse(HttpStatusCode.Forbidden, userNotAllowed);
                if (user == null || user.Trim().Length == 0)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, userIsNull);
                if (range == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, rangeIsNull);
                return Request.CreateResultResponse<IEnumerable<TimeKeepEntry>>(TimeKeepEntry.ReadByUser(user, range));
            }
            catch(Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Gets the list of time keep entries for a given a case number and time range
        /// </summary>
        /// <param name="user">The case number</param>
        /// <param name="range">The range</param>
        /// <returns></returns>
        [HttpPost]
        [Route("TimeKeepEntries/Case/{casenumber}")]
        public HttpResponseMessage GetByCase(string casenumber, [FromBody] DateRange range)
        {
            try
            {
                if (casenumber == null || casenumber.Trim().Length == 0)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, CaseNumberIsNull2);
                if (range == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, rangeIsNull);
                return Request.CreateResultResponse<IEnumerable<TimeKeepEntry>>(TimeKeepEntry.ReadByCase(casenumber, GetPrincipalUser(RequestContext.Principal), range));
            }
            catch (Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Gets the list of time keep entries for a given a case number and time range
        /// </summary>
        /// <param name="user">The case number</param>
        /// <param name="range">The range</param>
        /// <returns></returns>
        [HttpPost]
        [Route("TimeKeepEntries/Case/{casenumber}/Totals")]
        public HttpResponseMessage GetByCaseTotals(string casenumber, [FromBody] DateRange range)
        {
            try
            {
                if (casenumber == null || casenumber.Trim().Length == 0)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, CaseNumberIsNull2);
                if (range == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, rangeIsNull);
                IEnumerable<TimeKeepEntry> entries = TimeKeepEntry.ReadByCase(casenumber, GetPrincipalUser(RequestContext.Principal), range);
                return Request.CreateResultResponse<TimeSpan>(TimeSpan.FromTicks(entries.Sum(c => c.LaborTS.Value.Ticks)));
            }
            catch (Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("TimeKeepEntries/Case/{casenumber}/LogAndDetailAll")]
        public HttpResponseMessage LogAndDetailAllByCase(string casenumber, [FromBody] DateRange range)
        {
            try
            {
                if (casenumber == null || casenumber.Trim().Length == 0)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, CaseNumberIsNull2);
                if (range == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, rangeIsNull);
                IEnumerable<TimeKeepEntry> entries = TimeKeepEntry.LogAndDetailAllByCase(casenumber, GetPrincipalUser(RequestContext.Principal), range);
                return Request.CreateResultResponse<IEnumerable<TimeKeepEntry>>(entries);
            }
            catch (Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Gets the totals for a user given a user and a time range
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="range">The range</param>
        /// <returns>A list of totals, including the global totals, totals per scorecard labor and per individual category</returns>
        [HttpPost]
        [Route("TimeKeepEntries/User/{user:alpha}/Totals")]
        public HttpResponseMessage GetByUserTotals(string user, [FromBody] DateRange range)
        {
            try
            {
                if (!ValidateUser(RequestContext.Principal, user))
                    return Request.CreateCustomErrorResponse(HttpStatusCode.Forbidden, userNotAllowed);
                if (user == null || user.Trim().Length == 0)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, userIsNull);
                if (range == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, rangeIsNull);
                IEnumerable<TimeKeepEntry> entries = TimeKeepEntry.ReadByUser(user, range);
                var validEntries = entries.Where(c => c.EndTime.HasValue && !c.Category.IsOut);

                List<TotalsResult> output = new List<TotalsResult>
            {
                new TotalsResult
                {
                    Category = new Category
                    {
                        ID = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                        Description = "Total Labor",
                        IsOut = false,
                        IsScorecard = true
                    },
                    TotalLabor = TimeSpan.FromTicks(validEntries.Sum(c => c.LaborTS.Value.Ticks))
                },

                new TotalsResult
                {
                    Category = new Category
                    {
                        ID = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        Description = "Scorecard Labor",
                        IsOut = false,
                        IsScorecard = true
                    },
                    TotalLabor = TimeSpan.FromTicks(validEntries.Where(c => c.Category.IsScorecard).Sum(c => c.LaborTS.Value.Ticks))
                },

                new TotalsResult
                {
                    Category = new Category
                    {
                        ID = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        Description = "Non-scorecard Labor",
                        IsOut = false,
                        IsScorecard = true
                    },
                    TotalLabor = TimeSpan.FromTicks(validEntries.Where(c => !c.Category.IsScorecard).Sum(c => c.LaborTS.Value.Ticks))
                }
            };
                output.AddRange(validEntries.GroupBy(d => d.Category)
                    .Select(e => new TotalsResult
                    {
                        Category = e.Key,
                        TotalLabor = TimeSpan.FromTicks(e.Sum(f => f.LaborTS.Value.Ticks))
                    }).OrderBy(c => c.Category));


                return Request.CreateResultResponse<IEnumerable<TotalsResult>>(output);
            }
            catch(Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        /// <summary>
        /// Gets a time keep entry, given an ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("TimeKeepEntries/{id:guid}")]
        public HttpResponseMessage Get(Guid id)
        {
            try
            {
                TimeKeepEntry entry = TimeKeepEntry.Read(id);
                if (!ValidateUser(RequestContext.Principal, entry))
                    return Request.CreateCustomErrorResponse(HttpStatusCode.Forbidden, userNotAllowed);
                if (entry == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.NotFound, noEntries);
                return Request.CreateResultResponse<TimeKeepEntry>(entry);
            }
            catch(Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Creates a new time keep entry 
        /// </summary>
        /// <param name="value">The time keep entry to save</param>
        /// <returns>The response message will contain a CreatedResult instance that will include the entry that was
        /// saved and potentially the last entry modified. See remarks for more information.</returns>
        /// <remarks>
        /// When a time keep entry is saved, it is assuming you're stopping the previous entry. The goal here is to treat this application as a stop-watch lap.
        /// If this event took place, the CreatedResult will have a Modified field providing data about the previous entry.
        /// </remarks>
        [HttpPost]
        public HttpResponseMessage Post([FromBody]TimeKeepEntry value)
        {

            try
            {
                if (!ValidateUser(RequestContext.Principal, value))
                    return Request.CreateCustomErrorResponse(HttpStatusCode.Forbidden, userNotAllowed);
                if (value == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, MissingOrInvalidRequestBody);
                if (value.User == null || value.User.Trim().Length == 0)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, userIsNull);

                if (value.ID.HasValue)
                {
                    if (value.Category == null)
                        return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, CategoryIsNull);

                    if (value.Category.IsScorecard && (string.IsNullOrEmpty(value.CaseNumber) || string.IsNullOrWhiteSpace(value.CaseNumber)))
                        return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, CaseNumberIsNull);

                    if (!value.Category.IsScorecard && !string.IsNullOrEmpty(value.CaseNumber) && !string.IsNullOrEmpty(value.CaseNumber))
                        return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, CaseNumberIsNotNull);
                }

                TimeKeepEntry modified = value.Create();
                CreatedResult responseValue = new CreatedResult()
                {
                    Modified = modified,
                    New = value
                };

                HttpResponseMessage response = Request.CreateResultResponse<CreatedResult>(HttpStatusCode.Created, responseValue);
                response.Headers.Location = new Uri(string.Format("{0}://{1}{2}/timekeepentries/{3}", Request.RequestUri.Scheme,
                    Request.RequestUri.Host, Request.RequestUri.IsDefaultPort ? string.Empty : (":" + Request.RequestUri.Port.ToString()),
                    value.ID));
                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Updates a time keep entry
        /// </summary>
        /// <param name="value">The new valuels</param>
        /// <returns>Updated time keep entry</returns>
        [HttpPut]
        public HttpResponseMessage Put([FromBody] TimeKeepEntry value)
        {
            try
            {
                if (!ValidateUser(RequestContext.Principal, value))
                    return Request.CreateCustomErrorResponse(HttpStatusCode.Forbidden, userNotAllowed);
                if (value == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, MissingOrInvalidRequestBody);
                if (!value.ID.HasValue)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, idIsNull);
                if (value.User == null || value.User.Trim().Length == 0)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, userIsNull);
                if (value.EndTime.HasValue && value.EndTime.Value <= value.StartTime)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, EndDateAtOrBeforeStartDate);


                if (value.Category == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, CategoryIsNull);

                if (value.Category.IsScorecard && (string.IsNullOrEmpty(value.CaseNumber) || string.IsNullOrWhiteSpace(value.CaseNumber)))
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, CaseNumberIsNull);

                if (!value.Category.IsScorecard && !string.IsNullOrEmpty(value.CaseNumber) && !string.IsNullOrEmpty(value.CaseNumber))
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, CaseNumberIsNotNull);

                TimeKeepEntry find = TimeKeepEntry.Read(value.ID.Value);
                if(find == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.NotFound, noEntries);

                value.Update();
                return Request.CreateResultResponse<TimeKeepEntry>(value);
            }
            catch (Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Deletes the given time keep entry
        /// </summary>
        /// <param name="value">The value to delete. NOTE: It could potentially only have the ID</param>
        /// <returns>The deleted value (just in case :).</returns>
        [HttpDelete]
        public HttpResponseMessage Delete([FromBody] TimeKeepEntry value)
        {
            try
            {
                if (!ValidateUser(RequestContext.Principal, value))
                    return Request.CreateCustomErrorResponse(HttpStatusCode.Forbidden, userNotAllowed);
                if (value == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, MissingOrInvalidRequestBody);
                if (!value.ID.HasValue)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, idIsNull);

                TimeKeepEntry find = TimeKeepEntry.Read(value.ID.Value);
                if (find == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.NotFound, noEntries);

                value.Delete();
                return Request.CreateResultResponse<TimeKeepEntry>(value);
            }
            catch (Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Toggles the IsLogged flag of a given time keep entry
        /// </summary>
        /// <param name="id">The ID</param>
        /// <returns>Updated time keep entry</returns>
        [HttpPatch]
        [Route("TimeKeepEntries/{id:guid}/Toggle/IsLogged")]
        public HttpResponseMessage ToggleIsLogged(Guid id)
        {
            try
            {
                TimeKeepEntry entry = TimeKeepEntry.Read(id);
                if (!ValidateUser(RequestContext.Principal, entry))
                    return Request.CreateCustomErrorResponse(HttpStatusCode.Forbidden, userNotAllowed);
                if (entry == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.NotFound, noEntries);

                entry.ToggleIsLogged();
                return Request.CreateResultResponse<TimeKeepEntry>(entry);
            }
            catch (Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Toggles the IsLogged flag of a given time keep entry
        /// </summary>
        /// <param name="id">The ID</param>
        /// <returns>Updated time keep entry</returns>
        [HttpPatch]
        [Route("TimeKeepEntries/{id:guid}/Toggle/IsDetailed")]
        public HttpResponseMessage ToggleIsDetailed(Guid id)
        {
            try
            {
                TimeKeepEntry entry = TimeKeepEntry.Read(id);
                if (!ValidateUser(RequestContext.Principal, entry))
                    return Request.CreateCustomErrorResponse(HttpStatusCode.Forbidden, userNotAllowed);
                if (entry == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.NotFound, noEntries);

                entry.ToggleIsDetailed();
                return Request.CreateResultResponse<TimeKeepEntry>(entry);
            }
            catch (Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}
