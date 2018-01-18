using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using TimeKeep.Web.API.Models;

namespace TimeKeep.Web.API.Controllers
{
    /// <summary>
    /// Represents an API endpoint for the Categories
    /// </summary>
    [Route("Categories")]
    [Authorize]
    public class CategoriesController : ApiController
    {
        /// <summary>
        /// Gets a list of categories
        /// </summary>
        /// <returns>IEnumerable&lt;Categories&gt; of Category object representing all the categories</returns>
        public HttpResponseMessage GetAllCategories()
        {
            try
            {
                return Request.CreateResultResponse<IEnumerable<Category>>(Category.Categories);
            }
            catch(Exception ex)
            {
                return Request.CreateCustomErrorResponse(System.Net.HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}
