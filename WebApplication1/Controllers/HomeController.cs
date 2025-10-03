using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using MySqlConnector;

/// Routes for the landing page (Home/Index) and Privacy page (Home/Privacy)
/// This controller does not handle forms or database interactions, that is done in ObstacleController.

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // Database connection string
        private readonly string _connectionString;

        // Constructor to initialize the connection string
        public HomeController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

   
        // This action displays the home page
        public IActionResult Index()
        {
            return View();
        }
        


        // This action displays the Privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // responese cache attribute to prevent caching of error responses
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        // This action handles errors and displays the Error view
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
