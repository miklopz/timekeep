using Microsoft.Web.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using TimeKeep.Web.API.Models;

namespace TimeKeep.Web.API.Controllers._2018_02_01
{
    /// <summary>
    /// Represents an API endpoint for the Categories
    /// </summary>
    [Route("Categories")]
    [ApiVersion("2018-02-01")]
    [Authorize]
    public class CategoriesController : Controllers.CategoriesController
    {
    }
}
