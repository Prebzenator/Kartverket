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

        /// <summary>
        /// Initializes the AdminController with required services.
        /// </summary>
        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        // GET: /Admin/CreateUser
        // Displays the create user form (System Administrator only)
        [Authorize(Roles = "System Administrator")]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        // POST: /Admin/CreateUser
        // Creates a new user with a temporary password
        // Assigns roles based on input, with safety checks and rollback if role assignment fails
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "System Administrator")]
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

            // Generate a temporary password for the new user
            var tempPassword = "Temp!" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var createResult = await _userManager.CreateAsync(user, tempPassword);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            // Determine roles to assign
            var rolesToAdd = new List<string>();
            if (string.IsNullOrWhiteSpace(vm.Role))
            {
                // Default role if none specified
                rolesToAdd.Add("Pilot");
            }
            else if (vm.Role == "System Administrator")
            {
                // Ensure System Administrators also get Registry Administrator role
                rolesToAdd.Add("System Administrator");
                rolesToAdd.Add("Registry Administrator");
            }
            else
            {
                rolesToAdd.Add(vm.Role);
            }

            // Safety check:
            // Validate that roles exist before assignment
            foreach (var role in rolesToAdd.Distinct())
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    // Rollback: delete user if role does not exist
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError("", $"Role '{role}' does not exist.");
                    return View(vm);
                }
            }

            // Assign roles, rollback if any assignment fails
            foreach (var role in rolesToAdd.Distinct())
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, role);
                if (!addRoleResult.Succeeded)
                {
                    // Rollback: remove user if role assignment fails
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError("", string.Join("; ", addRoleResult.Errors.Select(e => e.Description)));
                    return View(vm);
                }
            }

            // Prepare success view model with temporary password
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

        // GET: /Admin/ManageUsers
        // Lists existing users with their roles
        // Provides delete action (System Administrator only)
        [Authorize(Roles = "System Administrator")]
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var vm = new ManageUsersViewModel
            {
                Users = new List<AdminUserListItemViewModel>()
            };

            // Build user list with roles
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                vm.Users.Add(new AdminUserListItemViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Organization = u.Organization,
                    Roles = string.Join(", ", roles)
                });
            }

            return View(vm);
        }

        // POST: /Admin/DeleteUser
        // Deletes a user with safety checks:
        // - Cannot delete yourself
        // - Cannot delete the last System Administrator
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "System Administrator")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Prevent deleting yourself
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Prevent deleting the last System Administrator
            // In theory not possible via UI, but double-check here
            if (await _userManager.IsInRoleAsync(user, "System Administrator"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("System Administrator");
                if (admins.Count <= 1)
                {
                    TempData["Error"] = "Cannot delete the last System Administrator account.";
                    return RedirectToAction(nameof(ManageUsers));
                }
            }

            // Attempt to delete user
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["Success"] = $"User {user.Email} deleted.";
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        // GET: /Admin/AdminMap
        // Displays the AdminMap view with approved obstacles
        // Accessible to Registry Administrators
        [HttpGet]
        public async Task<IActionResult> AdminMap()
        {
            // Load only approved obstacles for display
            var approved = await _db.Obstacles
                .Where(o => o.Status == ReportStatus.Approved)
                .AsNoTracking()
                .ToListAsync();

            return View(approved);
        }
    }
}