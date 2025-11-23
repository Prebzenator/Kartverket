using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using WebApplication1.Data;
using WebApplication1.Helpers;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Controller for handling obstacle data input and admin review.
    /// Supports both draft (NotApproved) and submitted (Pending) reports.
    /// Requires user authentication.
    /// </summary>
    [Authorize]
    public class ObstacleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Constructor injecting database context and user manager.
        /// </summary>
        public ObstacleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// GET: /Obstacle/DataForm
        /// Displays the form for entering obstacle data.
        /// If 'missing' parameter is true, prefills ObstacleName with "MISSING - ".
        /// </summary>
        /// <param name="missing">If true, prefills the name for a missing obstacle report.</param>
        [HttpGet]
        public IActionResult DataForm(bool missing = false)
        {
            var model = new ObstacleData();

            if (missing)
            {
                // Prefill name for missing obstacle reports
                model.ObstacleName = "MISSING - ";
                ViewBag.Missing = true;
            }

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
        /// POST: /Obstacle/DataForm
        /// Handles form submission for creating or updating obstacle reports.
        /// Supports both creating new reports and editing existing ones.
        /// Can save as draft (NotApproved) or submit for review (Pending).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataForm(Microsoft.AspNetCore.Http.IFormCollection form, string? IsDraft, int? id)
        {
            // Parse draft flag
            var isDraft = string.Equals(IsDraft, "true", System.StringComparison.OrdinalIgnoreCase);

            // Get current authenticated user
            var user = await _userManager.GetUserAsync(User);

            // Helper: parse raw input into decimal? (culture‑safe)
            decimal? ParseDecimalRaw(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                raw = raw.Trim();
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv)) return inv;
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out var cur)) return cur;
                return null;
            }

            // Parse height input
            var heightRaw = form["HeightInputRaw"].FirstOrDefault();
            var parsedHeightInput = ParseDecimalRaw(heightRaw);

            decimal? heightMetersFromInput = null;
            if (parsedHeightInput.HasValue)
            {
                if (User.IsInRole("Pilot"))
                {
                    // Convert feet to meters for pilots
                    heightMetersFromInput = UnitConverter.ToMeters(parsedHeightInput);
                }
                else
                {
                    heightMetersFromInput = parsedHeightInput;
                }
            }

            ObstacleData? obstacledata;

            if (id.HasValue && id.Value > 0)
            {
                /// <summary>
                /// ===== EDITING EXISTING REPORT =====
                /// Finds the report by ID, checks ownership, and updates fields.
                /// </summary>
                obstacledata = await _context.Obstacles.FindAsync(id.Value);
                if (obstacledata == null) return NotFound();

                if (user != null && obstacledata.ReportedByUserId != user.Id)
                {
                    return Forbid();
                }

                obstacledata.ObstacleName = form["ObstacleName"];
                obstacledata.ObstacleHeight = heightMetersFromInput ?? obstacledata.ObstacleHeight;
                obstacledata.ObstacleDescription = form["ObstacleDescription"];
                obstacledata.Latitude = decimal.TryParse(form["Latitude"], out var lat) ? lat : obstacledata.Latitude;
                obstacledata.Longitude = decimal.TryParse(form["Longitude"], out var lng) ? lng : obstacledata.Longitude;

                /// <summary>
                /// ✅ Save full geometry from hidden field
                /// </summary>
                obstacledata.GeometryJson = form["GeometryJson"];

                obstacledata.ReportedAt = DateTime.UtcNow;

                if (int.TryParse(form["CategoryId"], out var categoryId))
                {
                    obstacledata.CategoryId = categoryId;
                }
            }
            else
            {
                /// <summary>
                /// ===== CREATING NEW REPORT =====
                /// Creates a new ObstacleData entity and assigns all fields.
                /// </summary>
                obstacledata = new ObstacleData
                {
                    ObstacleName = form["ObstacleName"],
                    ObstacleHeight = heightMetersFromInput,
                    ObstacleDescription = form["ObstacleDescription"],
                    Latitude = ParseDecimalRaw(form["Latitude"]),
                    Longitude = ParseDecimalRaw(form["Longitude"]),
                    GeometryJson = form["GeometryJson"],   // ✅ Save full geometry
                    ReportedAt = DateTime.UtcNow,
                    DateData = DateTime.UtcNow
                };

                if (user != null)
                {
                    obstacledata.ReportedByUserId = user.Id;
                    obstacledata.ReporterName = user.FullName;
                    obstacledata.ReporterOrganization = user.Organization;
                }

                if (int.TryParse(form["CategoryId"], out var categoryId))
                {
                    obstacledata.CategoryId = categoryId;
                }

                _context.Obstacles.Add(obstacledata);
            }

            /// <summary>
            /// Set status based on draft flag:
            /// - Draft: NotApproved
            /// - Submit: Pending
            /// </summary>
            obstacledata.Status = isDraft ? ReportStatus.NotApproved : ReportStatus.Pending;

            if (!isDraft && !TryValidateModel(obstacledata))
            {
                return View(obstacledata);
            }

            await _context.SaveChangesAsync();

            if (isDraft)
            {
                TempData["SuccessMessage"] = "Report saved as draft. You can edit it from 'My Reports'.";
            }

            return View("Overview", obstacledata);
        }

        /// <summary>
        /// Legacy redirect: Admin review page moved to AdminObstacleController.
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
        /// Redirects to admin dashboard where proper approve/reject actions exist.
        /// </summary>
        [Authorize(Roles = "Registry Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, ReportStatus status)
        {
            return RedirectToAction("Dashboard", "AdminObstacle");
        }
    }
}
