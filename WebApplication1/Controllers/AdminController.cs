using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// System Administrator controller for user management.
    /// Also exposes AdminMap so Registry Administrators can open the map view.
    /// </summary>
    [Authorize(Roles = "System Administrator,Registry Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
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
                EmailConfirmed = true,
                MustChangePassword = true
            };

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


        /// Returns the AdminMap view with approved obstacles for Registry Administrators.

        [HttpGet]
        public async Task<IActionResult> AdminMap()
        {
            var approved = await _db.Obstacles
                .Where(o => o.Status == ReportStatus.Approved)
                .AsNoTracking()
                .ToListAsync();

            return View(approved);
        }
    }
}
