using System.ComponentModel.DataAnnotations;

namespace CMR.Models
{
    public class ReportComment
    {
        public ReportComment()
        {
        }

        public ReportComment(string content, Report report, ApplicationUser user)
        {
            Content = content;
            Report = report;
            User = user;
        }

        public int Id { get; set; }

        [DataType(DataType.Text)]
        public string Content { get; set; }

        public virtual Report Report { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}