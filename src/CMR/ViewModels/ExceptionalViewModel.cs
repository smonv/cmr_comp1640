using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CMR.Models;

namespace CMR.ViewModels
{
    public class ExceptionalViewModel
    {
        public int? SYear { get; set; }
        public List<Course> CoursesNoManagers { get; set; }
        public List<Course> CoursesNoCmr { get; set; } 
        public List<Report> NotApprovedReports { get; set; }  
    }
}