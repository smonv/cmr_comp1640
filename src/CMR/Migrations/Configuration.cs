namespace CMR.Migrations
{
    using Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using CMR.Models;
    using Microsoft.AspNet.Identity.EntityFramework;
    using Microsoft.AspNet.Identity;
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
        }

        public void SeedRole(ApplicationDbContext context)
        {
            var store = new RoleStore<IdentityRole>(context);
            var manager = new RoleManager<IdentityRole>(store);
            string[] roles = { "Administrator", "Staff", "Guest" };
            foreach (string role in roles)
            {
                manager.Create(new IdentityRole(role));
            }
        }

        public void SeedUser(ApplicationDbContext context)
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

            foreach(ApplicationUser guest in guests)
            {
                var result = manager.Create(guest, "password");
                if (result.Succeeded)
                {
                    manager.AddToRole(guest.Id, "Guest");
                }
            }
        }
    }
}
