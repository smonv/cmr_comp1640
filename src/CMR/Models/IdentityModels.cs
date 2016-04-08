using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace CMR.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string Fullname { get; set; }

        public virtual ICollection<FacultyAssignmentManager> FacultyAssignments { get; set; }
        public virtual ICollection<CourseAssignmentManager> CourseAssignments { get; set; }
        public virtual ICollection<ReportComment> ReportComments { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            userIdentity.AddClaim(new Claim("Fullname", Fullname));
            return userIdentity;
        }

        public string GetDisplayName()
        {
            return Fullname.IsNullOrWhiteSpace() ? Fullname : UserName;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", false)
        {
        }

        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<FacultyAssignment> FacultyAssignments { get; set; }
        public DbSet<FacultyAssignmentManager> FacultyAssignmentManagers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseAssignment> CourseAssignments { get; set; }
        public DbSet<CourseAssignmentManager> CourseAssignmentManagers { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportStatistical> ReportStatistical { get; set; }
        public DbSet<ReportDistribution> ReportDistribution { get; set; }
        public DbSet<ReportComment> ReportComments { get; set; }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}