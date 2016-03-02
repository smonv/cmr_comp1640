using System;
using System.ComponentModel.DataAnnotations;

namespace CMR.Models
{
    public class Report
    {
        public int Id { get; set; }
        public string Title { get; set; }
        [DataType(DataType.Text)]
        public string Content { get; set; }
        public Boolean IsApproved { get; set; }

        public virtual CourseAssignment Course { get; set; }
    }
}