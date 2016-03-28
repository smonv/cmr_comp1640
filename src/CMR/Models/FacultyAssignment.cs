using System.Collections.Generic;

namespace CMR.Models
{
    public class FacultyAssignment
    {
        public FacultyAssignment()
        {
        }

        public FacultyAssignment(Faculty faculty)
        {
            Faculty = faculty;
        }

        public int Id { get; set; }
        public virtual Faculty Faculty { get; set; }
        public virtual ICollection<FacultyAssignmentManager> Managers { get; set; }
    }
}