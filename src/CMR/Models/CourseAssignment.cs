using System;
using System.Collections.Generic;

namespace CMR.Models
{
    public class CourseAssignment
    {
        public CourseAssignment()
        {
        }

        public CourseAssignment(Course course, DateTime start, DateTime end)
        {
            Course = course;
            Start = start;
            End = end;
        }

        public int Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public virtual Course Course { get; set; }
        public virtual ICollection<Report> Reports { get; set; }
        public virtual ICollection<CourseAssignmentManager> Managers { get; set; }
    }
}