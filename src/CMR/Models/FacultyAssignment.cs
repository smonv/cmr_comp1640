using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMR.Models
{
    public class FacultyAssignment
    {
        public int Id { get; set; }
        public virtual Faculty Faculty { get; set; }
        public virtual ICollection<FacultyAssignmentManager> Managers { get; set; } 

        public FacultyAssignment() { }

        public FacultyAssignment(Faculty faculty)
        {
            Faculty = faculty;
        }
    }
}