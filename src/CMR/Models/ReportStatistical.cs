using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMR.Models
{
    public class ReportStatistical
    {
        public int Id { get; set; }
        public int Mean { get; set; }
        public int Median { get; set; }
        public string Type { get; set; }
        public virtual Report Report { get; set; }

        public ReportStatistical() { }

        public ReportStatistical(int mean, int median, string type, Report report)
        {
            this.Mean = mean;
            this.Median = median;
            this.Type = type;
            this.Report = report;
        }
    }
}