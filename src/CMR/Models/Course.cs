using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMR.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }

        public virtual ICollection<CourseAssignment> Managers { get; set; }
        public virtual ICollection<Faculty> Faculties { get; set; }
    }
}