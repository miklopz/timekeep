using Microsoft.Web.Http.Routing;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace TimeKeep.Web.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            EnableCorsAttribute cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);
            config.MapHttpAttributeRoutes();
            config.AddApiVersioning(o => o.ReportApiVersions = true);

            IsoDateTimeConverter converter = new IsoDateTimeConverter
            {
                DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"
            };

            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(converter);

            config.Routes.MapHttpRoute(
                "VersionedQueryString",
                "{controller}/{id}",
                defaults: null);
            
            config.Routes.MapHttpRoute(
                "VersionedUrl",
                "v{apiVersion}/{controller}/{id}",
                defaults: null,
                constraints: new { apiVersion = new ApiVersionRouteConstraint() });
        }
    }
}
