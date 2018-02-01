using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeKeep.Web.API.Models._2018_02_01
{
    public class CaseTotalsResponse
    {
        public TimeSpan TotalLabor { get; set; }
        public TimeSpan TotalUnloggedLabor { get; set; }
    }
}