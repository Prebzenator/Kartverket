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
    /// Supports draft reports that can be edited before submission.
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
        /// If 'missing' is true, prefill ObstacleName with "MISSING - ".
        /// </summary>
        [HttpGet]
        public IActionResult DataForm(bool missing = false)
        {
            var model = new ObstacleData();

            if (missing)
            {
                model.ObstacleName = "MISSING - ";
                ViewBag.Missing = true;
            }

            return View(model);
        }

        /// <summary>
        /// Handles form submission, validates input, saves to database, and shows overview.
        /// Supports both draft and final submission.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataForm(IFormCollection form, string? IsDraft, int? id)
        {
            var isDraft = string.Equals(IsDraft, "true", System.StringComparison.OrdinalIgnoreCase);
            var user = await _userManager.GetUserAsync(User);

            ObstacleData obstacledata;
            if (id.HasValue && id.Value > 0)
            {
                // Editing existing report
                obstacledata = await _context.Obstacles.FindAsync(id.Value);
                if (obstacledata == null) return NotFound();

                // Security: Only allow editing if user owns the report
                if (user != null && obstacledata.ReportedByUserId != user.Id)
                {
                    return Forbid();
                }

                // Security: Only allow editing drafts
                if (!obstacledata.IsDraft)
                {
                    TempData["ErrorMessage"] = "Cannot edit a report that has been submitted for review.";
                    return RedirectToAction("Log", "Pilot");
                }

                // Update fields
                obstacledata.ObstacleName = form["ObstacleName"];
                obstacledata.ObstacleHeight = decimal.TryParse(form["ObstacleHeight"], out var h) ? h : obstacledata.ObstacleHeight;
                obstacledata.ObstacleDescription = form["ObstacleDescription"];
                obstacledata.Latitude = decimal.TryParse(form["Latitude"], out var lat) ? lat : obstacledata.Latitude;
                obstacledata.Longitude = decimal.TryParse(form["Longitude"], out var lng) ? lng : obstacledata.Longitude;
                obstacledata.ReportedAt = DateTime.UtcNow;
            }
            else
            {
                // Creating new report
                obstacledata = new ObstacleData
                {
                    ObstacleName = form["ObstacleName"],
                    ObstacleHeight = decimal.TryParse(form["ObstacleHeight"], out var height) ? height : default,
                    ObstacleDescription = form["ObstacleDescription"],
                    Latitude = decimal.TryParse(form["Latitude"], out var lat) ? lat : (decimal?)null,
                    Longitude = decimal.TryParse(form["Longitude"], out var lng) ? lng : (decimal?)null,
                    ReportedAt = DateTime.UtcNow,
                    DateData = DateTime.UtcNow
                };

                if (user != null)
                {
                    obstacledata.ReportedByUserId = user.Id;
                    obstacledata.ReporterName = user.FullName;
                    obstacledata.ReporterOrganization = user.Organization;
                }

                _context.Obstacles.Add(obstacledata);
            }

            // Set draft status and report status
            obstacledata.IsDraft = isDraft;
            obstacledata.Status = isDraft ? ReportStatus.Pending : ReportStatus.Pending;

            // When not a draft, validate the model
            if (!isDraft && !TryValidateModel(obstacledata))
            {
                return View(obstacledata);
            }

            await _context.SaveChangesAsync();

            // Show different messages based on draft status
            if (isDraft)
            {
                TempData["SuccessMessage"] = "Report saved as draft. You can edit it from 'My Reports'.";
            }

            return View("Overview", obstacledata);
        }

        /// <summary>
        /// Admin: displays all obstacle reports regardless of status.
        /// Moved to AdminObstacleController - kept here for backwards compatibility.
        /// </summary>
        [Authorize(Roles = "Registry Administrator")]
        [HttpGet]
        public IActionResult Review()
        {
            return RedirectToAction("Dashboard", "AdminObstacle");
        }

        /// <summary>
        /// Admin: displays only pending obstacle reports.
        /// Moved to AdminObstacleController - kept here for backwards compatibility.
        /// </summary>
        [Authorize(Roles = "Registry Administrator")]
        [HttpGet]
        public IActionResult ReviewPending()
        {
            return RedirectToAction("Dashboard", "AdminObstacle", new { filterStatus = "Pending" });
        }

        /// <summary>
        /// Admin: updates the status of a report (Approve or Reject).
        /// Moved to AdminObstacleController - kept here for backwards compatibility.
        /// </summary>
        [Authorize(Roles = "Registry Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ReportStatus status)
        {
            return RedirectToAction("Dashboard", "AdminObstacle");
        }
    }
}