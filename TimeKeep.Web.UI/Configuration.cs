using System.Configuration;

namespace TimeKeep.Web.UI
{
    public static class Configuration
    {
        public static class Cache
        {
            public static string CSSVersion
            {
                get
                {
                    return ConfigurationManager.AppSettings["Cache:CSSVersion"] ?? "0";
                }
            }
            public static string JSVersion
            {
                get
                {
                    return ConfigurationManager.AppSettings["Cache:JSVersion"] ?? "0";
                }
            }
        }

        public static class API
        {
            public static string Endpoint
            {
                get
                {
                    return ConfigurationManager.AppSettings["API:Endpoint"];
                }
            }
            public static string ApiVersion
            {
                get
                {
                    return ConfigurationManager.AppSettings["API:ApiVersion"] ?? "2017-09-01";
                }
            }
        }

        public static class OAuth
        {
            public static string ReplyURL
            {
                get { return ConfigurationManager.AppSettings["OAuth:ReplyURL"]; }
            }
            public static string AuthEndpoint
            {
                get { return ConfigurationManager.AppSettings["OAuth:AuthEndpoint"]; }
            }
            public static string ClientID
            {
                get { return ConfigurationManager.AppSettings["OAuth:ClientID"]; }
            }
            public static string TokenEndpoint
            {
                get { return ConfigurationManager.AppSettings["OAuth:TokenEndpoint"]; }
            }
            public static string Resource
            {
                get { return ConfigurationManager.AppSettings["OAuth:Resource"]; }
            }
            public static string Secret
            {
                get { return ConfigurationManager.AppSettings["OAuth:Secret"]; }
            }
        }
    }
}