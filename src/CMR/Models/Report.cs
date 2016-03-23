using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMR.Models
{
    public class Report
    {
        public Report()
        {
        }

        public Report(int totalStudent, string action)
        {
            TotalStudent = totalStudent;
            Action = action;
            IsApproved = false;
            CreateAt = DateTime.Now;
        }

        public int Id { get; set; }
        public bool IsApproved { get; set; }
        public int TotalStudent { get; set; }

        [DataType(DataType.Text)]
        public string Action { get; set; }

        [Required]
        public DateTime CreateAt { get; set; }

        public virtual CourseAssignment Assignment { get; set; }
        public virtual ICollection<ReportStatistical> Statisticals { get; set; }
        public virtual ICollection<ReportDistribution> Distributions { get; set; }
        public virtual ICollection<ReportComment> Comments { get; set; }
    }
}