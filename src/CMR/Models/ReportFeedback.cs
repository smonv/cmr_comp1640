using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CMR.Models
{
    public class ReportFeedback
    {
        public int Id { get; set; }
        [DataType(DataType.Text)]
        public string Content { get; set; }

        public virtual Report Report { get; set; }

        public ReportFeedback() { }

        public ReportFeedback(string content, Report report)
        {
            this.Content = content;
            this.Report = report;
        }
    }
}