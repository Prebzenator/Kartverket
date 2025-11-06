using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public static class RoleSeeder
    {
        // Call this once at startup (dev only) to create roles + initial admin
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = new[] { "Pilot", "Registry Administrator" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Create initial admin if env variables are provided (or fallback to safe defaults)
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
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Registry Administrator");
                }
            }
            else
            {
                // Update existing admin user with name and org if they're missing
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

                if (!await userManager.IsInRoleAsync(admin, "Registry Administrator"))
                    await userManager.AddToRoleAsync(admin, "Registry Administrator");
            }
        }
    }
}