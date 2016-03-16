using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNet.Identity;

namespace CMR.Models
{
    public class Report
    {
        public int Id { get; set; }
        public bool IsApproved { get; set; }
        public int TotalStudent { get; set; }
        [DataType(DataType.Text)]
        public string Comment { get; set; }
        [DataType(DataType.Text)]
        public string Action { get; set; }

        public virtual CourseAssignment Assignment { get; set; }
        public virtual ICollection<ReportStatistical> Statisticals { get; set; }
        public virtual ICollection<ReportDistribution> Distributions { get; set; }
        public virtual ICollection<ReportFeedback> Feedbacks { get; set; }

        public Report() { }

        public Report(int totalStudent, string comment, string action)
        {
            this.TotalStudent = totalStudent;
            this.Comment = comment;
            this.Action = action;
            this.IsApproved = false;
        }
    }
}