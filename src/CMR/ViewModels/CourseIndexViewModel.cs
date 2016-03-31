using System.Collections.Generic;
using CMR.Models;

namespace CMR.ViewModels
{
    public class CourseIndexViewModel
    {
        public List<Course> Courses { get; set; }
        public List<Faculty> Faculties { get; set; }
        public int? SYear { get; set; }
    }
}