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
    /// Simple admin UI for creating users for demo/provisioning.
    /// Only users in "Registry Administrator" role can access.
    /// </summary>
    [Authorize(Roles = "Registry Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: /Admin/CreateUser
        public IActionResult CreateUser()
        {
            // Provide a simple view model. The Role select can be hard-coded in the view.
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
                EmailConfirmed = true // for demo, skip email confirmation
            };

            // Generate a simple temporary password that meets policy.
            var tempPassword = "Temp!" + Guid.NewGuid().ToString("N").Substring(0, 8); // Temp! + 8 chars

            // Create user with temp password
            var createResult = await _userManager.CreateAsync(user, tempPassword);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            // Add to selected role (expects "Pilot" or "Registry Administrator")
            if (!string.IsNullOrWhiteSpace(vm.Role))
            {
                await _userManager.AddToRoleAsync(user, vm.Role);
            }

            // For the mockup: show the temp password to the admin so they can give it to the user.
            // In production you would send a secure email/invite link instead.
            var successVm = new CreateUserSuccessViewModel
            {
                Email = vm.Email,
                Role = vm.Role,
                TempPassword = tempPassword
            };

            return View("CreateUserSuccess", successVm);
        }

        // Optional: Users list, delete, or role management can be added later.
    }
}
