using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Data;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Handles routes for obstacle data input and admin review.
    ///
    /// Routes:
    /// - GET /Obstacle/DataForm: Displays the form for entering obstacle data.
    /// - POST /Obstacle/DataForm: Handles form submission, validates input, saves to database, and shows overview.
    /// - GET /Obstacle/Review: Displays all submitted reports for admin review.
    /// - GET /Obstacle/ReviewPending: Displays only pending reports.
    /// - POST /Obstacle/UpdateStatus: Updates the status of a report (Approve/Reject).
    ///
    /// Notes:
    /// - Uses Entity Framework Core via ApplicationDbContext to persist obstacle reports.
    /// - Model validation is enforced via data annotations in ObstacleData.cs.
    /// - LoggedAt timestamp is automatically set at object creation.
    /// - User info is automatically captured from the logged-in user.
    /// - Admin-only routes are protected via role-based authorization.
    /// </summary>
    [Authorize]
    public class ObstacleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ObstacleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Displays the form for entering obstacle data.
        /// </summary>
        [HttpGet]
        public IActionResult DataForm()
        {
            return View();
        }

        /// <summary>
        /// Handles form submission, validates input, saves to database, and shows overview.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DataForm(ObstacleData obstacledata)
        {
            if (!ModelState.IsValid)
            {
                return View(obstacledata);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                obstacledata.ReportedByUserId = user.Id;
                obstacledata.ReporterName = user.FullName;
                obstacledata.ReporterOrganization = user.Organization;
            }

            obstacledata.Status = ReportStatus.Pending;
            obstacledata.ReportedAt = DateTime.UtcNow;

            _context.Obstacles.Add(obstacledata);
            await _context.SaveChangesAsync();

            return View("Overview", obstacledata);
        }

        /// <summary>
        /// Admin: displays all obstacle reports regardless of status.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Review()
        {
            var allObstacles = _context.Obstacles
                .OrderByDescending(o => o.ReportedAt)
                .ToList();

            return View(allObstacles);
        }

        /// <summary>
        /// Admin: displays only pending obstacle reports.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult ReviewPending()
        {
            var pendingObstacles = _context.Obstacles
                .Where(o => o.Status == ReportStatus.Pending)
                .OrderByDescending(o => o.ReportedAt)
                .ToList();

            return View("Review", pendingObstacles);
        }

        /// <summary>
        /// Admin: updates the status of a report (Approve or Reject).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ReportStatus status)
        {
            var obstacle = await _context.Obstacles.FindAsync(id);
            if (obstacle == null)
            {
                return NotFound();
            }

            obstacle.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction("Review");
        }
    }
}
