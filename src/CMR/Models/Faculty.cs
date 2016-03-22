using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMR.Models
{
    public class Faculty
    {
        public int Id { get; set; }
        [Index(IsUnique = true)]
        [StringLength(200)]
        public string Name { get; set; }
        public virtual ICollection<FacultyAssignment> FacultyAssignment { get; set; }
        public virtual ICollection<Course> Courses { get; set; }
    }
}