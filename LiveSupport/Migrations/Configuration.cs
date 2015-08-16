namespace LiveSupport.Migrations
{
    using LiveSupport.Models;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<LiveSupport.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(LiveSupport.Models.ApplicationDbContext context)
        {
            var userStore = new UserStore<ApplicationUser>(context);
            var userManager = new UserManager<ApplicationUser>(userStore);

            if (!context.Roles.Any(x => x.Name == "Administrator")){
                var adminRole = new IdentityRole {
                    Name = "Administrator"
                };
                context.Roles.Add(adminRole);
            }

            if (!context.Roles.Any(x => x.Name == "Agent")){
                var agentRole = new IdentityRole{
                    Name = "Agent"
                };
                context.Roles.Add(agentRole);
            }
            context.SaveChanges();

            if (!context.Users.Any(x => x.UserName == "quocminhdo92@gmail.com")){
                var user = new ApplicationUser {
                    UserName = "quocminhdo92@gmail.com",
                    Email = "quocminhdo92@gmail.com",
                };
                userManager.Create(user, "123456");
                userManager.AddToRole(user.Id, "Administrator");
                userManager.AddToRole(user.Id, "Agent");
            }

            if (!context.Users.Any(x => x.UserName == "agent@test.com")){
                var user = new ApplicationUser
                {
                    UserName = "agent@test.com",
                    Email = "agent@test.com"
                };
                userManager.Create(user, "123456");
                userManager.AddToRole(user.Id, "Agent");
            }

            if (!context.Users.Any(x => x.UserName == "user@test.com"))
            {
                var user = new ApplicationUser
                {
                    UserName = "user@test.com",
                    Email = "user@test.com"
                };
                userManager.Create(user, "123456");
            }
            context.SaveChanges();
        }
    }
}
