using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMR.Models
{
    public class ReportNotification
    {
        public int Id { get; set; }
        public bool Read { get; set; }
        public string Message { get; set; }
        public virtual Report Report { get; set; }
    }
}