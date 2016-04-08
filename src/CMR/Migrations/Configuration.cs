using System.Data.Entity.Migrations;
using System.Linq;
using System.Security.Claims;
using CMR.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace CMR.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ApplicationDbContext context)
        {
            base.Seed(context);
            SeedRole(context);
            SeedUser(context);
            SeedFaculties(context);
            SeedCourse(context);
        }

        public void SeedRole(ApplicationDbContext context)
        {
            if (context.Roles.Any()) return;
            var store = new RoleStore<IdentityRole>(context);
            var manager = new RoleManager<IdentityRole>(store);
            string[] roles = {"Administrator", "Staff", "Guest"};
            foreach (var role in roles)
            {
                manager.Create(new IdentityRole(role));
            }
        }

        public void SeedUser(ApplicationDbContext context)
        {
            if (context.Users.Any()) return;
            var store = new UserStore<ApplicationUser>(context);
            var manager = new UserManager<ApplicationUser>(store);

            ApplicationUser[] admins =
            {
                new ApplicationUser {UserName = "admin1", Fullname = "Admin One", Email = "admin1@test.com"},
                new ApplicationUser {UserName = "admin2", Fullname = "Admin Two", Email = "admin2@test.com"},
                new ApplicationUser {UserName = "admin3", Fullname = "Admin Three", Email = "admin3@test.com"}
            };

            foreach (var admin in from admin in admins let result = manager.Create(admin, "password") where result.Succeeded select admin)
            {
                manager.AddToRole(admin.Id, "Administrator");
            }

            ApplicationUser[] staffs =
            {
                new ApplicationUser {UserName = "staff1", Fullname = "Staff One", Email = "staff1@test.com"},
                new ApplicationUser {UserName = "staff2", Fullname = "Staff Two", Email = "staff2@test.com"},
                new ApplicationUser {UserName = "staff3", Fullname = "Staff Three", Email = "staff3@test.com"},
                new ApplicationUser {UserName = "staff4", Fullname = "Staff Four", Email = "staff4@test.com"},
                new ApplicationUser {UserName = "staff5", Fullname = "Staff Five", Email = "staff5@test.com"},
                new ApplicationUser {UserName = "staff6", Fullname = "Staff Six", Email = "staff6@test.com"},
                new ApplicationUser {UserName = "staff7", Fullname = "Staff Seven", Email = "staff7@test.com"},
                new ApplicationUser {UserName = "staff8", Fullname = "Staff Eight", Email = "staff8@test.com"},
                new ApplicationUser {UserName = "staff9", Fullname = "Staff Nine", Email = "staff9@test.com"}
            };

  
            foreach (var staff in from staff in staffs let result = manager.Create(staff, "password") where result.Succeeded select staff)
            {
                manager.AddToRole(staff.Id, "Staff");
            }

            ApplicationUser[] guests =
            {
                new ApplicationUser {UserName = "guest1", Fullname = "Guest One", Email = "guest1@test.com"},
                new ApplicationUser {UserName = "guest2", Fullname = "Guest Two", Email = "guest2@test.com"},
                new ApplicationUser {UserName = "guest3", Fullname = "Guest Three", Email = "guest3@test.com"}
            };

            foreach (var guest in from guest in guests let result = manager.Create(guest, "password") where result.Succeeded select guest)
            {
                manager.AddToRole(guest.Id, "Guest");
            }
        }

        public void SeedFaculties(ApplicationDbContext context)
        {
            if (context.Faculties.Any()) return;
            Faculty[] faculties =
            {
                new Faculty {Name = "Accounting and Management", Description = ".."},
                new Faculty {Name = "Business, Government and the International Economy", Description = ".."},
                new Faculty {Name = "Entrepreneurial Management", Description = ".."},
                new Faculty {Name = "Finance", Description = ".."},
                new Faculty {Name = "Marketing", Description = ".."},
                new Faculty {Name = "Negotiation, Organizations & Markets", Description = ".."},
                new Faculty {Name = "Organizational Behavior", Description = ".."},
                new Faculty {Name = "Strategy", Description = ".."},
                new Faculty {Name = "Technology and Operations Management", Description = ".."},
                new Faculty {Name = "General Management", Description = ".."}
            };
            foreach (var f in faculties)
            {
                context.Faculties.AddOrUpdate(f);
            }
        }

        public void SeedCourse(ApplicationDbContext context)
        {
            if (context.Courses.Any()) return;
            Course[] courses =
            {
                new Course {Code = "COMP1640", Name = "Enterprise Web Software Development", Description = ".."},
                new Course {Code = "COMP1648", Name = "Development, Frameworks and Methods", Description = ".."},
                new Course {Code = "COMP1639", Name = "Database Engineering", Description = ".."},
                new Course {Code = "COMP1649", Name = "Interaction Design", Description = ".."},
                new Course
                {
                    Code = "COMP1661",
                    Name = "Application Development for Mobile Devices",
                    Description = ".."
                },
                new Course {Code = "COMP1689", Name = "Programming Frameworks", Description = ".."},
                new Course
                {
                    Code = "COMP1108",
                    Name = "Project (Computing) - for External Programmes",
                    Description = ".."
                }
            };

            foreach (var c in courses)
            {
                context.Courses.Add(c);
            }
        }
    }
}