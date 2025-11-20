using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// AccountController: Register, Login, Logout, ForceChangePassword, ChangePassword.
    /// Public registration can be toggled via config.
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
                MustChangePassword = false // Self-registered users don't need forced change
            };

            var createResult = await _userManager.CreateAsync(user, vm.Password);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            if (!await _roleManager.RoleExistsAsync("Pilot"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Pilot"));
            }

            await _userManager.AddToRoleAsync(user, "Pilot");
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(vm.Email);
                if (user != null && user.MustChangePassword)
                {
                    return RedirectToAction("ForceChangePassword");
                }

                if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                    return Redirect(vm.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(vm);
        }

        // GET: /Account/ForceChangePassword
        [HttpGet]
        public IActionResult ForceChangePassword()
        {
            return View(new ForceChangePasswordViewModel());
        }

        // POST: /Account/ForceChangePassword
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

                // UPDATED: Success message
                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);

            return View(vm);
        }

        // GET: /Account/ChangePassword (My Account)
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
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

                // UPDATED: Success message
                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);

            return View(vm);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}