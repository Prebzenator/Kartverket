using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Uses ASP.NET for authentication and user management
    /// Manages user registration, login, logout, and password changes
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 IConfiguration configuration,
                                 RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
        }

// GET: /Account/Register
// Checks if self-registration is allowed via configuration.
// If allowed, displays the registration form.
        [HttpGet]
        public IActionResult Register()
        {
            bool allow = _configuration.GetValue<bool>("Authentication:AllowSelfRegistration");
            if (!allow)
            {
                return NotFound();
            }

            return View(new RegisterViewModel());
        }

// POST: /Account/Register 
// Creates new user account
// Validates input, creates user, assigns "Pilot" role, and signs in the user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            bool allow = _configuration.GetValue<bool>("Authentication:AllowSelfRegistration");
            if (!allow) return NotFound();

            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FullName = vm.FullName,
                Organization = vm.Organization,
                EmailConfirmed = true,
                MustChangePassword = false
            };

            var createResult = await _userManager.CreateAsync(user, vm.Password);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }
// Ensure "Pilot" role exists and assign it to the new user
            if (!await _roleManager.RoleExistsAsync("Pilot"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Pilot"));
            }

            await _userManager.AddToRoleAsync(user, "Pilot");
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

// GET: /Account/Login
// Shows the login page
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }
// POST: /Account/Login
// Validates user credentials and logs in the user
// Forces password change if required
// If the login fails, an error message is shown
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(vm.Email);

// Force password change if required
                if (user != null && user.MustChangePassword)
                {
                    return RedirectToAction("ForceChangePassword");
                }
// Redirect to return URL if specified and local, else to home page
                if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                    return Redirect(vm.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(vm);
        }
// GET: /Account/ForceChangePassword
// Displays the force change password page
// Forcces new users to change their password from the temporary one
        [HttpGet]
        public IActionResult ForceChangePassword()
        {
            return View(new ForceChangePasswordViewModel());
        }

// POST: /Account/ForceChangePassword
// Handles the force change password form submission
// Validates input, changes the password, and updates the user's MustChangePassword flag
// If successful, redirects to the home page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceChangePassword(ForceChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, vm.OldPassword, vm.NewPassword);
            if (result.Succeeded)
            {
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);

                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);

            return View(vm);
        }

// GET: /Account/ChangePassword
// Displays the change password page for logged-in users
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

// POST: /Account/ChangePassword
// Refreshes the user's password/change login cookie
// if failed show error messages
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, vm.OldPassword, vm.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);

            return View(vm);
        }


// POST: /Account/Logout
// Signs out the current user and redirects to the home page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}