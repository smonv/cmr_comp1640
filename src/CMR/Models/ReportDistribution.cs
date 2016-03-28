using System.ComponentModel.DataAnnotations;

namespace CMR.Models
{
    public class ReportDistribution
    {
        public ReportDistribution()
        {
        }

        public ReportDistribution(int bad, int average, int good, string Type, Report report)
        {
            Bad = bad;
            Average = average;
            Good = good;
            this.Type = Type;
            Report = report;
        }

        public int Id { get; set; }

        [Range(0, 5000)]
        public int Bad { get; set; }

        [Range(0, 5000)]
        public int Average { get; set; }

        [Range(0, 5000)]
        public int Good { get; set; }

        public string Type { get; set; }
        public virtual Report Report { get; set; }
    }
}