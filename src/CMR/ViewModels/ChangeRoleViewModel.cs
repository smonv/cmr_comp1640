using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CMR.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace CMR.ViewModels
{
    public class ChangeRoleViewModel
    {
        public ApplicationUser User { get; set; }
        public List<IdentityRole> Roles { get; set; } 
    }
}