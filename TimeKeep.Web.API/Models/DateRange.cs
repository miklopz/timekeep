using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeKeep.Web.API.Models
{
    public sealed class DateRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}