using System.Collections.Generic;
using CMR.Models;

namespace CMR.ViewModels
{
    public class FacultyCourseModel
    {
        public Course Course { get; set; }
        public List<Faculty> Faculties { get; set; }
    }
}