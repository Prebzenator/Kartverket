using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Handles the main entry points of the application:
    /// Index, registry administrator redirection and error page.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes the HomeController with database context.
        /// </summary>
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Home/Index
        // Displays the home page
        // If the user is authenticated and has the "Registry Administrator" role,
        // redirect them to the Admin Dashboard instead
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Registry Administrator"))
            {
                // Registry Administrators should land directly on their dashboard
                return RedirectToAction("Dashboard", "AdminObstacle");
            }

            // Fetch all obstacles that are approved and have valid coordinates
            // Only obstacles with Latitude and Longitude are included
            var obstacles = await _context.Obstacles
                .Where(o => o.Status == ReportStatus.Approved &&
                            o.Latitude.HasValue &&
                            o.Longitude.HasValue)
                .ToListAsync();

            // Pass the obstacles list to the view for rendering
            return View(obstacles);
        }

        // GET: /Home/Privacy
        // Displays the Privacy page
        // We currently don't use the privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: /Home/Error
        // Displays the Error page with diagnostic information
        // ResponseCache disabled to ensure fresh error details are shown
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Build error view model with current request ID
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
