using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CMR.Models;

namespace CMR.ViewModels
{
    public class FacultyAssignmentModel
    {
        public Faculty Faculty { get; set; }
        public List<ApplicationUser> Staffs { get; set; }
    }
}