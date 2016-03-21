using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;

namespace CMR.Models
{
    public class Course
    {
        public int Id { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Code { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; }

        public virtual ICollection<CourseAssignment> Managers { get; set; }
        public virtual ICollection<Faculty> Faculties { get; set; }
    }
}