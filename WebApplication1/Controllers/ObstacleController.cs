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
    /// All actions require user authentication.
    /// Supports both draft (NotApproved) and submitted (Pending) reports.
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
        /// If 'missing' parameter is true, prefills ObstacleName with "MISSING - ".
        /// This is used when reporting obstacles that should be in the registry but aren't.
        /// </summary>
        /// <param name="missing">If true, prefill form for a missing obstacle report</param>
        [HttpGet]
        public IActionResult DataForm(bool missing = false)
        {
            var model = new ObstacleData();

            if (missing)
            {
                // Prefill name for missing obstacle reports
                // User cannot remove this prefix (enforced in view JavaScript)
                model.ObstacleName = "MISSING - ";
                ViewBag.Missing = true;
            }

            return View(model);
        }

        /// <summary>
        /// Handles form submission for creating or updating obstacle reports.
        /// Supports both creating new reports and editing existing ones.
        /// Can save as draft (NotApproved) or submit for review (Pending).
        /// </summary>
        /// <param name="form">Form data collection from the POST request</param>
        /// <param name="IsDraft">String "true" if saving as draft, anything else for submission</param>
        /// <param name="id">Optional: ID of existing report to update (null for new reports)</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataForm(IFormCollection form, string? IsDraft, int? id)
        {
            // Parse draft flag - case-insensitive check for "true"
            var isDraft = string.Equals(IsDraft, "true", System.StringComparison.OrdinalIgnoreCase);

            // Get current authenticated user for ownership tracking
            var user = await _userManager.GetUserAsync(User);

            ObstacleData obstacledata;

            if (id.HasValue && id.Value > 0)
            {
                // ===== EDITING EXISTING REPORT =====
                obstacledata = await _context.Obstacles.FindAsync(id.Value);
                if (obstacledata == null) return NotFound();

                // Security check: Only the report owner can edit their own reports
                if (user != null && obstacledata.ReportedByUserId != user.Id)
                {
                    return Forbid(); // Return 403 Forbidden
                }

                // Update all editable fields from the form
                obstacledata.ObstacleName = form["ObstacleName"];
                obstacledata.ObstacleHeight = decimal.TryParse(form["ObstacleHeight"], out var h) ? h : obstacledata.ObstacleHeight;
                obstacledata.ObstacleDescription = form["ObstacleDescription"];
                obstacledata.Latitude = decimal.TryParse(form["Latitude"], out var lat) ? lat : obstacledata.Latitude;
                obstacledata.Longitude = decimal.TryParse(form["Longitude"], out var lng) ? lng : obstacledata.Longitude;

                // Update timestamp to reflect the edit
                obstacledata.ReportedAt = DateTime.UtcNow;
            }
            else
            {
                // ===== CREATING NEW REPORT =====
                obstacledata = new ObstacleData
                {
                    ObstacleName = form["ObstacleName"],
                    ObstacleHeight = decimal.TryParse(form["ObstacleHeight"], out var height) ? height : default,
                    ObstacleDescription = form["ObstacleDescription"],
                    Latitude = decimal.TryParse(form["Latitude"], out var lat) ? lat : (decimal?)null,
                    Longitude = decimal.TryParse(form["Longitude"], out var lng) ? lng : (decimal?)null,
                    ReportedAt = DateTime.UtcNow,
                    DateData = DateTime.UtcNow // Set creation timestamp (never changes)
                };

                // Cache user information for performance and audit trail
                if (user != null)
                {
                    obstacledata.ReportedByUserId = user.Id;
                    obstacledata.ReporterName = user.FullName;
                    obstacledata.ReporterOrganization = user.Organization;
                }

                _context.Obstacles.Add(obstacledata);
            }

            // Set status based on draft flag:
            // - Draft (save for later): Status = NotApproved (no admin interaction yet)
            // - Submit (ready for review): Status = Pending (enters admin review queue)
            obstacledata.Status = isDraft ? ReportStatus.NotApproved : ReportStatus.Pending;

            // Validation: Only validate required fields when submitting (not for drafts)
            // This allows users to save incomplete drafts without validation errors
            if (!isDraft && !TryValidateModel(obstacledata))
            {
                // Return to form with validation errors if submission fails
                return View(obstacledata);
            }

            await _context.SaveChangesAsync();

            // Show appropriate success message based on action taken
            if (isDraft)
            {
                TempData["SuccessMessage"] = "Report saved as draft. You can edit it from 'My Reports'.";
            }

            // Redirect to overview page showing the submitted/saved report
            return View("Overview", obstacledata);
        }

        /// <summary>
        /// Legacy redirect: Admin review page moved to AdminObstacleController.
        /// Kept for backwards compatibility with old bookmarks/links.
        /// Redirects to the new unified admin dashboard.
        /// </summary>
        [Authorize(Roles = "Registry Administrator")]
        [HttpGet]
        public IActionResult Review()
        {
            return RedirectToAction("Dashboard", "AdminObstacle");
        }

        /// <summary>
        /// Legacy redirect: Review pending reports moved to AdminObstacleController.
        /// Kept for backwards compatibility with old bookmarks/links.
        /// Redirects to admin dashboard with Pending filter applied.
        /// </summary>
        [Authorize(Roles = "Registry Administrator")]
        [HttpGet]
        public IActionResult ReviewPending()
        {
            return RedirectToAction("Dashboard", "AdminObstacle", new { filterStatus = "Pending" });
        }

        /// <summary>
        /// Legacy action: Status updates moved to AdminObstacleController.
        /// Kept for backwards compatibility with old forms.
        /// Redirects to admin dashboard where proper approve/reject actions exist.
        /// </summary>
        [Authorize(Roles = "Registry Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ReportStatus status)
        {
            // Redirect to dashboard - actual approve/reject logic is in AdminObstacleController
            return RedirectToAction("Dashboard", "AdminObstacle");
        }
    }
}