namespace CMR.Migrations
{
    using Models;
    using System.Data.Entity.Migrations;
    using Microsoft.AspNet.Identity.EntityFramework;
    using Microsoft.AspNet.Identity;
    using System.Linq;
    internal sealed class Configuration : DbMigrationsConfiguration<CMR.Models.ApplicationDbContext>
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
            if (!context.Roles.Any())
            {
                var store = new RoleStore<IdentityRole>(context);
                var manager = new RoleManager<IdentityRole>(store);
                string[] roles = { "Administrator", "Staff", "Guest" };
                foreach (string role in roles)
                {
                    manager.Create(new IdentityRole(role));
                }
            }
        }

        public void SeedUser(ApplicationDbContext context)
        {
            if (!context.Users.Any())
            {
                var store = new UserStore<ApplicationUser>(context);
                var manager = new UserManager<ApplicationUser>(store);

                ApplicationUser[] admins =
                {
                new ApplicationUser { UserName = "admin1", Email = "admin1@test.com" },
                new ApplicationUser { UserName = "admin2", Email = "admin2@test.com" },
                new ApplicationUser { UserName = "admin3", Email = "admin3@test.com" }
            };

                foreach (ApplicationUser admin in admins)
                {
                    var result = manager.Create(admin, "password");
                    if (result.Succeeded)
                    {
                        manager.AddToRole(admin.Id, "Administrator");
                    }
                }

                ApplicationUser[] staffs =
                {
                new ApplicationUser { UserName = "staff1", Email = "staff1@test.com" },
                new ApplicationUser { UserName = "staff2", Email = "staff2@test.com" },
                new ApplicationUser { UserName = "staff3", Email = "staff3@test.com" }
            };

                foreach (ApplicationUser staff in staffs)
                {
                    var result = manager.Create(staff, "password");
                    if (result.Succeeded)
                    {
                        manager.AddToRole(staff.Id, "Staff");
                    }
                }

                ApplicationUser[] guests =
                {
                new ApplicationUser { UserName = "guest1", Email = "guest1@test.com" },
                new ApplicationUser { UserName = "guest2", Email = "guest2@test.com" },
                new ApplicationUser { UserName = "guest3", Email = "guest3@test.com" },
            };

                foreach (ApplicationUser guest in guests)
                {
                    var result = manager.Create(guest, "password");
                    if (result.Succeeded)
                    {
                        manager.AddToRole(guest.Id, "Guest");
                    }
                }
            }
        }

        public void SeedFaculties(ApplicationDbContext context)
        {
            if (!context.Faculties.Any())
            {
                Faculty[] faculties =
                {
                new Faculty { Name = "Accounting and Management" },
                new Faculty { Name = "Business, Government and the International Economy" },
                new Faculty { Name = "Entrepreneurial Management" },
                new Faculty { Name = "Finance" },
                new Faculty { Name = "Marketing" },
                new Faculty { Name = "Negotiation, Organizations & Markets" },
                new Faculty { Name = "Organizational Behavior" },
                new Faculty { Name = "Strategy" },
                new Faculty { Name = "Technology and Operations Management" },
                new Faculty { Name = "General Management" }
            };
                foreach (Faculty f in faculties)
                {
                    context.Faculties.AddOrUpdate(f);
                }
            }
        }

        public void SeedCourse(ApplicationDbContext context)
        {
            if (!context.Courses.Any())
            {
                Course[] courses =
                {
                new Course { Code = "COMP1640", Name = "Enterprise Web Software Development" },
                new Course { Code = "COMP1648", Name = "Development, Frameworks and Methods" },
                new Course { Code = "COMP1639", Name = "Database Engineering" },
                new Course { Code = "COMP1649", Name = "Interaction Design" },
                new Course { Code = "COMP1661", Name = "Application Development for Mobile Devices" },
                new Course { Code = "COMP1689", Name = "Programming Frameworks" },
                new Course { Code = "COMP1108", Name = "Project (Computing) - for External Programmes" }
            };

                foreach (Course c in courses)
                {
                    context.Courses.Add(c);
                }
            }
        }
    }
}
