using System.Collections.Generic;
using CMR.Models;

namespace CMR.ViewModels
{
    public class UserRoleViewModel
    {
        public ApplicationUser User { get; set; }
        public List<string> Roles { get; set; }
    }
}