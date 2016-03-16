using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMR.Models
{
    public class ReportDistribution
    {
        public int Id { get; set; }
        public int Bad { get; set; }
        public int Average { get; set; }
        public int Good { get; set; }
        public string Type { get; set; }
        public virtual Report Report { get; set; }

        public ReportDistribution() { }

        public ReportDistribution(int bad, int average, int good, string Type, Report report)
        {
            this.Bad = bad;
            this.Average = average;
            this.Good = good;
            this.Type = Type;
            this.Report = report;
        }
    }
}