using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = new[] { "Pilot", "Registry Administrator", "System Administrator" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@example.com";
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin123!";
            var adminName = Environment.GetEnvironmentVariable("ADMIN_NAME") ?? "System Administrator";
            var adminOrg = Environment.GetEnvironmentVariable("ADMIN_ORG") ?? "Kartverket";

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = adminName,
                    Organization = adminOrg,
                    EmailConfirmed = true,
                    MustChangePassword = true // Force password change on first login
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "System Administrator");
                    await userManager.AddToRoleAsync(admin, "Registry Administrator");
                }
            }
            else
            {
                var currentRoles = await userManager.GetRolesAsync(admin);

                if (!currentRoles.Contains("System Administrator"))
                {
                    await userManager.AddToRoleAsync(admin, "System Administrator");
                }

                if (!currentRoles.Contains("Registry Administrator"))
                {
                    await userManager.AddToRoleAsync(admin, "Registry Administrator");
                }

                bool needsUpdate = false;
                if (string.IsNullOrEmpty(admin.FullName))
                {
                    admin.FullName = adminName;
                    needsUpdate = true;
                }
                if (string.IsNullOrEmpty(admin.Organization))
                {
                    admin.Organization = adminOrg;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    await userManager.UpdateAsync(admin);
                }
            }

            var oldAdminRole = await roleManager.FindByNameAsync("Admin");
            if (oldAdminRole != null)
            {
                var usersInOldRole = await userManager.GetUsersInRoleAsync("Admin");
                foreach (var user in usersInOldRole)
                {
                    if (!await userManager.IsInRoleAsync(user, "Registry Administrator"))
                    {
                        await userManager.AddToRoleAsync(user, "Registry Administrator");
                    }
                    await userManager.RemoveFromRoleAsync(user, "Admin");
                }
                await roleManager.DeleteAsync(oldAdminRole);
            }

            var allUsers = userManager.Users.ToList();
            foreach (var user in allUsers)
            {
                var userRoles = await userManager.GetRolesAsync(user);
                if (userRoles.Count == 0)
                {
                    await userManager.AddToRoleAsync(user, "Pilot");
                }
            }
        }
    }
}