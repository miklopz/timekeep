using System.Configuration;

namespace TimeKeep.Web.API
{
    public static class Configuration
    {
        internal static bool GetBoolean(string key, bool defaultValue)
        {
            string strValue = ConfigurationManager.AppSettings[key];
            if (strValue == null || strValue.Trim().Length == 0)
                return defaultValue;
            bool temp = defaultValue;
            if (bool.TryParse(strValue, out temp))
                return temp;
            return defaultValue;
        }
        internal static int GetInt(string key, int defaultValue)
        {
            string strValue = ConfigurationManager.AppSettings[key];
            if (strValue == null || strValue.Trim().Length == 0)
                return defaultValue;
            int temp = defaultValue;
            if (int.TryParse(strValue, out temp))
                return temp;
            return defaultValue;
        }

        public static class RetryPolicy
        {
            public static bool Enabled = GetBoolean("RetryPolicy:Enabled", true);
            public static int Retries = GetInt("RetryPolicy:Retries", 5);
            public static int Increment = GetInt("RetryPolicy:Increment", 2);
            public static int InitialWait = GetInt("RetryPolicy:InitialWait", 2);
        }
    }
}