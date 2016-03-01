using CMR.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMR.ViewModels
{
    public class FacultyCourseModel
    {
        public Course Course { get; set; }
        public List<Faculty> Faculties { get; set; }
    }
}