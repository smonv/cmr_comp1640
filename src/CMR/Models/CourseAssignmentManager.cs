using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMR.Models
{
    public class CourseAssignmentManager
    {
        public int Id { get; set; }
        public string Role { get; set; }
        public ApplicationUser User { get; set; }
        public CourseAssignment CourseAssignment { get; set; }

        public CourseAssignmentManager() { }

        public CourseAssignmentManager(string role, ApplicationUser user, CourseAssignment ca)
        {
            Role = role;
            User = user;
            CourseAssignment = ca;
        }
    }
}