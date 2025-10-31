using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Handles routes for obstacle data input and overview display (Obstacle registration).
    /// 
    /// Routes:
    /// - GET /Obstacle/DataForm: Displays the form for entering obstacle data.
    /// - POST /Obstacle/DataForm: Handles form submission, validates input, saves to database, and shows overview.
    /// - GET /Obstacle/Overview: Displays the overview of submitted obstacle data.
    /// 
    /// Notes:
    /// - Uses Entity Framework Core via ApplicationDbContext to persist obstacle reports.
    /// - Model validation is enforced via data annotations in ObstacleData.cs.
    /// - LoggedAt timestamp is automatically set at object creation.
    /// </summary>
    public class ObstacleController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes the controller with the application's database context.
        /// </summary>
        /// <param name="context">Injected EF Core database context.</param>
        public ObstacleController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Displays the form for entering obstacle data (GET).
        /// </summary>
        /// <returns>View with empty ObstacleData model.</returns>
        [HttpGet]
        public ActionResult DataForm()
        {
            return View();
        }

        /// <summary>
        /// Handles the form submission for obstacle data (POST).
        /// - Validates the model.
        /// - Saves the report to the database if valid.
        /// - Displays the overview page with submitted data.
        /// </summary>
        /// <param name="obstacledata">User-submitted obstacle data.</param>
        /// <returns>Overview view if valid; otherwise redisplays form with validation errors.</returns>
        [HttpPost]
        public async Task<IActionResult> DataForm(ObstacleData obstacledata)
        {
            if (!ModelState.IsValid)
            {
                return View(obstacledata);
            }

            _context.Obstacles.Add(obstacledata);
            await _context.SaveChangesAsync();

            return View("Overview", obstacledata);
        }
    }
}
