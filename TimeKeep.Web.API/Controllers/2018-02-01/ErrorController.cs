using Microsoft.Web.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TimeKeep.Web.API.Models;

namespace TimeKeep.Web.API.Controllers._2018_02_01
{
    [Authorize]
    [ApiVersion("2018-02-01")]
    /// <summary>
    /// Used as a controller for the Error model
    /// </summary>
    public class ErrorController : Controllers.ErrorController
    {
        /// <summary>
        /// Log a new error
        /// </summary>
        /// <param name="error">The error to log (from the body)</param>
        /// <returns>The same error, with an error ID and official timestamp</returns>
        public override HttpResponseMessage Post([FromBody] Error error)
        {
            try
            {
                if (string.IsNullOrEmpty(error.Type))
                {
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, noType);
                }
                if (string.IsNullOrEmpty(error.Message))
                {
                    return Request.CreateCustomErrorResponse(HttpStatusCode.BadRequest, noMessage);
                }
                error.Create();
                return Request.CreateResponse<Error>(HttpStatusCode.Created, error);
            }
            catch(Exception ex)
            {
                return Request.CreateCustomErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}
