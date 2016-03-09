using System;
using System.Collections.Generic;

namespace CMR.Models
{
    public class CourseAssignment
    {
        public int Id { get; set; }
        public string Role { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public virtual ApplicationUser Manager { get; set; }
        public virtual Course Course { get; set; }
        public virtual ICollection<Report> Reports { get; set; }
    }
}