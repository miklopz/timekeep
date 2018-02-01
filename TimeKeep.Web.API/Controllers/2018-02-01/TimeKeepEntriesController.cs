using Microsoft.Web.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Principal;
using System.Web.Http;
using TimeKeep.Web.API.Models;
using TimeKeep.Web.API.Models._2018_02_01;

namespace TimeKeep.Web.API.Controllers._2018_02_01
{
    [Authorize]
    [ApiVersion("2018-02-01")]
    [Route("TimeKeepEntries")]
    public class TimeKeepEntriesController : Controllers.TimeKeepEntriesController
    {
        /// <summary>
        /// Gets the list of time keep entries for a given user and time range
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="range">The range</param>
        /// <returns></returns>
        [HttpPost]
        [Route("TimeKeepEntries/User/{user:alpha}")]
        public override HttpResponseMessage GetByUser(string user, [FromBody] DateRange range)
        {
            return base.GetByUser(user, range);
        }

        /// <summary>
        /// Gets the list of time keep entries for a given a case number and time range
        /// </summary>
        /// <param name="user">The case number</param>
        /// <param name="range">The range</param>
        /// <returns></returns>
        [HttpPost]
        [Route("TimeKeepEntries/Case/{casenumber}")]
        public override HttpResponseMessage GetByCase(string casenumber, [FromBody] DateRange range)
        {
            return base.GetByCase(casenumber, range);
        }

        /// <summary>
        /// Gets the list of time keep entries for a given a case number and time range
        /// </summary>
        /// <param name="casenumber">The case number</param>
        /// <param name="range">The range</param>
        /// <returns></returns>
        [HttpPost]
        [Route("TimeKeepEntries/Case/{casenumber}/Totals")]
        /// <summary>
        /// Gets the list of time keep entries for a given a case number and time range
        /// </summary>
        /// <param name="user">The case number</param>
        /// <param name="range">The range</param>
        /// <returns>HttpResponseMessage</returns>
        public override HttpResponseMessage GetByCaseTotals(string casenumber, DateRange range)
        {
            try
            {
                if (casenumber == null || casenumber.Trim().Length == 0)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, CaseNumberIsNull2);
                if (range == null)
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, rangeIsNull);
                IEnumerable<TimeKeepEntry> entries = TimeKeepEntry.ReadByCase(casenumber, GetPrincipalUser(RequestContext.Principal), range);
                TimeSpan totalLabor = TimeSpan.FromTicks(entries.Sum(c => c.LaborTS.Value.Ticks));
                TimeSpan unloggedLabor = TimeSpan.FromTicks(entries.Where(c => !c.IsLogged).Sum(c => c.LaborTS.Value.Ticks));

                CaseTotalsResponse response = new CaseTotalsResponse { TotalLabor = totalLabor, TotalUnloggedLabor = unloggedLabor };

                return Request.CreateResultResponse<CaseTotalsResponse>(response);
            }
            catch (Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("TimeKeepEntries/Case/{casenumber}/LogAndDetailAll")]
        public override HttpResponseMessage LogAndDetailAllByCase(string casenumber, [FromBody] DateRange range)
        {
            return base.LogAndDetailAllByCase(casenumber, range);
        }

        /// <summary>
        /// Gets the totals for a user given a user and a time range
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="range">The range</param>
        /// <returns>A list of totals, including the global totals, totals per scorecard labor and per individual category</returns>
        [HttpPost]
        [Route("TimeKeepEntries/User/{user:alpha}/Totals")]
        public override HttpResponseMessage GetByUserTotals(string user, [FromBody] DateRange range)
        {
            return base.GetByUserTotals(user, range);
        }


        /// <summary>
        /// Gets a time keep entry, given an ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("TimeKeepEntries/{id:guid}")]
        public override HttpResponseMessage Get(Guid id)
        {
            return base.Get(id);
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
        public override HttpResponseMessage Post([FromBody]TimeKeepEntry value)
        {
            return base.Post(value);
        }

        /// <summary>
        /// Updates a time keep entry
        /// </summary>
        /// <param name="value">The new valuels</param>
        /// <returns>Updated time keep entry</returns>
        [HttpPut]
        public override HttpResponseMessage Put([FromBody] TimeKeepEntry value)
        {
            return base.Put(value);
        }

        /// <summary>
        /// Deletes the given time keep entry
        /// </summary>
        /// <param name="value">The value to delete. NOTE: It could potentially only have the ID</param>
        /// <returns>The deleted value (just in case :).</returns>
        [HttpDelete]
        public override HttpResponseMessage Delete([FromBody] TimeKeepEntry value)
        {
            return base.Delete(value);
        }

        /// <summary>
        /// Toggles the IsLogged flag of a given time keep entry
        /// </summary>
        /// <param name="id">The ID</param>
        /// <returns>Updated time keep entry</returns>
        [HttpPatch]
        [Route("TimeKeepEntries/{id:guid}/Toggle/IsLogged")]
        public override HttpResponseMessage ToggleIsLogged(Guid id)
        {
            return base.ToggleIsLogged(id);
        }

        /// <summary>
        /// Toggles the IsLogged flag of a given time keep entry
        /// </summary>
        /// <param name="id">The ID</param>
        /// <returns>Updated time keep entry</returns>
        [HttpPatch]
        [Route("TimeKeepEntries/{id:guid}/Toggle/IsDetailed")]
        public override HttpResponseMessage ToggleIsDetailed(Guid id)
        {
            return base.ToggleIsDetailed(id);
        }
    }
}
