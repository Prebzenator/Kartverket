using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Models;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // If the user is authenticated and has the "Registry Administrator" role,
            // redirect them to the Admin Dashboard
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Registry Administrator"))
            {
                return RedirectToAction("Dashboard", "AdminObstacle");
            }

            // Fetch all obstacles that are approved and have valid coordinates
            var obstacles = await _context.Obstacles
                .Where(o => o.Status == ReportStatus.Approved &&
                            o.Latitude.HasValue &&
                            o.Longitude.HasValue)
                .ToListAsync();

            // Pass the obstacles list to the view
            return View(obstacles);
        }

        // Display the Privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Display the Error page with diagnostic information
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}

