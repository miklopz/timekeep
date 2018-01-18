using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using TimeKeep.Web.API.Models;

namespace TimeKeep.Web.API
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_Error()
        {
            Exception ex = Server.GetLastError();
            Error err = TimeKeep.Web.API.Models.Error.Log(ex, 500);
            Response.Write(JsonConvert.SerializeObject(err));
            Response.StatusCode = 500;
            Response.StatusDescription = "Internal Server Error";
            return;
        }
    }
}