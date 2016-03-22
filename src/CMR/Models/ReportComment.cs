using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CMR.Models
{
    public class ReportComment
    {
        public int Id { get; set; }
        [DataType(DataType.Text)]
        public string Content { get; set; }

        public virtual Report Report { get; set; }
        public virtual ApplicationUser User { get; set; }

        public ReportComment() { }

        public ReportComment(string content, Report report, ApplicationUser user)
        {
            Content = content;
            Report = report;
            User = user;
        }
    }
}