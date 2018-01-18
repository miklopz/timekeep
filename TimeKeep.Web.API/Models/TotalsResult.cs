using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace TimeKeep.Web.API.Models
{
    public sealed class TotalsResult
    {
        public Category Category { get; set; }
        internal TimeSpan TotalLabor { get; set; }

        public string Labor
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (TotalLabor.Days > 0) {
                    sb.Append(TotalLabor.Days);
                    sb.Append(".");
                }
                sb.Append(TotalLabor.Hours);
                sb.Append(":");
                if (TotalLabor.Minutes < 10)
                    sb.Append("0");
                sb.Append(TotalLabor.Minutes);
                return sb.ToString();
            }
        }
    }
}