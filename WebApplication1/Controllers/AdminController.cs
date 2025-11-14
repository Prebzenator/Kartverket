using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// System Administrator controller for user management.
    /// Only users in "System Administrator" role can access.
    /// Separated from Registry Administrator functions.
    /// </summary>
    [Authorize(Roles = "System Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager,
                             RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Admin/CreateUser
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Create user object without password
            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FullName = vm.FullName,
                Organization = vm.Organization,
                EmailConfirmed = true, // for demo, skip email confirmation
                MustChangePassword = true // 👈 viktig: tving passordendring ved første login
            };

            // Generate a simple temporary password that meets policy.
            var tempPassword = "Temp!" + Guid.NewGuid().ToString("N").Substring(0, 8);

            // Create user with temp password
            var createResult = await _userManager.CreateAsync(user, tempPassword);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            // Ensure the selected role exists
            if (!string.IsNullOrWhiteSpace(vm.Role))
            {
                if (!await _roleManager.RoleExistsAsync(vm.Role))
                {
                    ModelState.AddModelError("", $"Role '{vm.Role}' does not exist.");
                    await _userManager.DeleteAsync(user); // Cleanup
                    return View(vm);
                }
                await _userManager.AddToRoleAsync(user, vm.Role);
            }
            else
            {
                // Default to Pilot if no role selected
                await _userManager.AddToRoleAsync(user, "Pilot");
            }

            // For the mockup: show the temp password to the admin
            var successVm = new CreateUserSuccessViewModel
            {
                FullName = vm.FullName,
                Organization = vm.Organization,
                Email = vm.Email,
                Role = vm.Role ?? "Pilot",
                TempPassword = tempPassword
            };

            return View("CreateUserSuccess", successVm);
        }

        // Optional future: GET /Admin/ManageUsers - list all users
        // Optional future: POST /Admin/DeleteUser/{id}
        // Optional future: POST /Admin/EditUser/{id}
    }
}
