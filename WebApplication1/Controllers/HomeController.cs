using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        // This action displays the home page
        // If user is Registry Administrator, redirect to admin dashboard
        public IActionResult Index()
        {
            // Check if user is a Registry Administrator
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Registry Administrator"))
            {
                return RedirectToAction("Dashboard", "AdminObstacle");
            }

            return View();
        }

        // This action displays the Privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Response cache attribute to prevent caching of error responses
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        // This action handles errors and displays the Error view
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}