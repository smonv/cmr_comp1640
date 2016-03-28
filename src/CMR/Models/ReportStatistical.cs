using System.ComponentModel.DataAnnotations;

namespace CMR.Models
{
    public class ReportStatistical
    {
        public ReportStatistical()
        {
        }

        public ReportStatistical(int mean, int median, string type, Report report)
        {
            Mean = mean;
            Median = median;
            Type = type;
            Report = report;
        }

        public int Id { get; set; }

        [Range(0, 5000)]
        public int Mean { get; set; }

        [Range(0, 5000)]
        public int Median { get; set; }

        public string Type { get; set; }
        public virtual Report Report { get; set; }
    }
}