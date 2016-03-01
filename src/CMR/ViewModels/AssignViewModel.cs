using CMR.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMR.ViewModels
{
    public class AssignViewModel
    {
        public Course Course { get; set; }
        public List<ApplicationUser> Staffs { get; set; }
    }
}