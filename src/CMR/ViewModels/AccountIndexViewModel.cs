using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity.EntityFramework;

namespace CMR.ViewModels
{
    public class AccountIndexViewModel
    {
        public List<IdentityRole> Roles { get; set; }
        public List<UserRoleViewModel> Urvms { get; set; } 
        public string FilterUsername { get; set; }
        public string FilterRole { get; set; }
    }
}