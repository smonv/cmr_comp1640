using System.Collections.Generic;
using CMR.Models;

namespace CMR.ViewModels
{
    public class AssignViewModel
    {
        public Course Course { get; set; }
        public List<ApplicationUser> Staffs { get; set; }
    }
}