using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMR.Models
{
    public class FacultyAssignment
    {
        public int Id { get; set; }
        public string Role { get; set; }
        public virtual ApplicationUser Staff { get; set; }
        public virtual Faculty Faculty { get; set; }
    }
}