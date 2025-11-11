using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Minimal AccountController: Register, Login, Logout.
    /// Keep it simple for the mockup. Public registration can be toggled via config.
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
        // Only show registration if allowed in configuration (development demo toggle).
        [HttpGet]
        public IActionResult Register()
        {
            bool allow = _configuration.GetValue<bool>("Authentication:AllowSelfRegistration");
            if (!allow)
            {
                // Not available in current configuration; show 404 so external users can't see
                // the page. For a nicer UX you could redirect to a "Request account" page.
                return NotFound();
            }

            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        // Creates a user and (for demo) assigns the "Pilot" role automatically.
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
                EmailConfirmed = true // For demo: skip confirmation to simplify testing
            };

            var createResult = await _userManager.CreateAsync(user, vm.Password);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            // Ensure Pilot role exists (harmless if it already does)
            if (!await _roleManager.RoleExistsAsync("Pilot"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Pilot"));
            }

            // For the mockup/demo: automatically put self-registered users into "Pilot".
            // In a real deployment you might require admin approval instead.
            await _userManager.AddToRoleAsync(user, "Pilot");

            // Sign the user in immediately (non-persistent cookie).
            await _signInManager.SignInAsync(user, isPersistent: false);

            // PRG pattern: redirect to Home after POST.
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
                if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                    return Redirect(vm.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
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
