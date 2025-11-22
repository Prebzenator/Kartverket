using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// System Administrator controller for user management.
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

        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FullName = vm.FullName,
                Organization = vm.Organization,
                EmailConfirmed = true, // Skip confirmation for this implementation
                MustChangePassword = true // Force password change on first login
            };

            // Generate a temporary password
            var tempPassword = "Temp!" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var createResult = await _userManager.CreateAsync(user, tempPassword);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.Role))
            {
                if (!await _roleManager.RoleExistsAsync(vm.Role))
                {
                    ModelState.AddModelError("", $"Role '{vm.Role}' does not exist.");
                    await _userManager.DeleteAsync(user);
                    return View(vm);
                }
                await _userManager.AddToRoleAsync(user, vm.Role);
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Pilot");
            }

            // Display the temporary password to the admin
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
    }
}