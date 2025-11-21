using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Data;
using WebApplication1.Helpers;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Globalization;

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

            // Populate category options for dropdown
            ViewBag.CategoryOptions = _context.ObstacleCategories
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

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
        public async Task<IActionResult> DataForm(Microsoft.AspNetCore.Http.IFormCollection form, string? IsDraft, int? id)
        {
            // Parse draft flag - case-insensitive check for "true"
            var isDraft = string.Equals(IsDraft, "true", System.StringComparison.OrdinalIgnoreCase);

            // Get current authenticated user for ownership tracking
            var user = await _userManager.GetUserAsync(User);

            // Helper: parse a raw input string into decimal? (robust against cultures)
            decimal? ParseDecimalRaw(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;

                raw = raw.Trim();
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv)) return inv;
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out var cur)) return cur;
                return null;
            }

            // Read the raw height input from the form (we use HeightInputRaw in the view)
            var heightRaw = form["HeightInputRaw"].FirstOrDefault();
            var parsedHeightInput = ParseDecimalRaw(heightRaw);

            // Convert parsed input to canonical meters depending on role
            decimal? heightMetersFromInput = null;
            if (parsedHeightInput.HasValue)
            {
                if (User.IsInRole("Pilot"))
                {
                    // input is in feet -> convert to meters
                    heightMetersFromInput = UnitConverter.ToMeters(parsedHeightInput);
                }
                else
                {
                    // input is in meters
                    heightMetersFromInput = parsedHeightInput;
                }
            }

            ObstacleData? obstacledata;

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
                obstacledata.ObstacleName = form["ObstacleName"].ToString() ?? string.Empty;

                // If user provided a height input we use the parsed/converted value, otherwise keep existing
                obstacledata.ObstacleHeight = heightMetersFromInput ?? obstacledata.ObstacleHeight;

                obstacledata.ObstacleDescription = form["ObstacleDescription"].FirstOrDefault();
                obstacledata.Latitude = decimal.TryParse(form["Latitude"], out var lat) ? lat : obstacledata.Latitude;
                obstacledata.Longitude = decimal.TryParse(form["Longitude"], out var lng) ? lng : obstacledata.Longitude;

                // Update timestamp to reflect the edit
                obstacledata.ReportedAt = DateTime.UtcNow;

                // Update category if provided
                if (int.TryParse(form["CategoryId"], out var categoryId))
                {
                    obstacledata.CategoryId = categoryId;
                }
            }
            else
            {
                // ===== CREATING NEW REPORT =====
                obstacledata = new ObstacleData
                {
                    ObstacleName = form["ObstacleName"].FirstOrDefault() ?? string.Empty,
                    // Use converted meters (may be null if no input)
                    ObstacleHeight = heightMetersFromInput,
                    ObstacleDescription = form["ObstacleDescription"],
                    Latitude = ParseDecimalRaw(form["Latitude"]),
                    Longitude = ParseDecimalRaw(form["Longitude"]),
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

                // Set category when creating
                if (int.TryParse(form["CategoryId"], out var categoryId))
                {
                    obstacledata.CategoryId = categoryId;
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
        public IActionResult UpdateStatus(int id, ReportStatus status)
        {
            // Redirect to dashboard - actual approve/reject logic is in AdminObstacleController
            return RedirectToAction("Dashboard", "AdminObstacle");
        }
    }
}