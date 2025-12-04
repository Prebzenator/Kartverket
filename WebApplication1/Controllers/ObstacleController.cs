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
    /// Handles the creation and modification of obstacle reports by users.
    /// </summary>
    [Authorize]
    public class ObstacleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Initializes the controller with database context and user manager.
        /// </summary>
        public ObstacleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Obstacle/DataForm
        // Displays the obstacle reporting form
        // If "missing" is true, pre-fills the name for a missing obstacle report
        [HttpGet]
        public IActionResult DataForm(bool missing = false)
        {
            var model = new ObstacleData();

            if (missing)
            {
                // Pre-fill the obstacle name to indicate a missing report
                model.ObstacleName = "MISSING - ";
                ViewBag.Missing = true;
            }

            // Shows category options
            ViewBag.CategoryOptions = _context.ObstacleCategories
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

            return View(model);
        }

        // POST: /Obstacle/DataForm
        // Processes the submission of an obstacle report (create or edit)
        // Supports draft saving via IsDraft=true
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataForm(Microsoft.AspNetCore.Http.IFormCollection form, string? IsDraft, int? id)
        {
            // Determine if the submission should be treated as a draft
            // Drafts skip full validation and are marked NotApproved
            var isDraft = string.Equals(IsDraft, "true", StringComparison.OrdinalIgnoreCase);

            var user = await _userManager.GetUserAsync(User);

            // Helper function to parse decimal values (height, latitude, longitude) from string
            decimal? ParseDecimalRaw(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                raw = raw.Trim();
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv)) return inv;
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out var cur)) return cur;
                return null;
            }

            // Parse height input from form
            var heightRaw = form["HeightInputRaw"].FirstOrDefault();
            var parsedHeightInput = ParseDecimalRaw(heightRaw);

            decimal? heightMetersFromInput = null;
            if (parsedHeightInput.HasValue)
            {
                // Pilots enter height in feet, convert to meters
                // Other roles enter directly in meters
                if (User.IsInRole("Pilot"))
                {
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
                // Editing an existing obstacle report
                obstacledata = await _context.Obstacles.FindAsync(id.Value);
                if (obstacledata == null) return NotFound();

                // Only the original reporter can edit their own report
                if (user != null && obstacledata.ReportedByUserId != user.Id)
                {
                    return Forbid();
                }

                // Update fields from form input
                obstacledata.ObstacleName = form["ObstacleName"].ToString() ?? string.Empty;
                obstacledata.ObstacleHeight = heightMetersFromInput ?? obstacledata.ObstacleHeight;
                obstacledata.ObstacleDescription = form["ObstacleDescription"].FirstOrDefault();
                obstacledata.Latitude = decimal.TryParse(form["Latitude"], out var lat) ? lat : obstacledata.Latitude;
                obstacledata.Longitude = decimal.TryParse(form["Longitude"], out var lng) ? lng : obstacledata.Longitude;
                obstacledata.ReportedAt = DateTime.UtcNow;

                if (int.TryParse(form["CategoryId"], out var categoryId))
                {
                    obstacledata.CategoryId = categoryId;
                }
            }
            else
            {
                // Creating a new obstacle report
                obstacledata = new ObstacleData
                {
                    ObstacleName = form["ObstacleName"].FirstOrDefault() ?? string.Empty,
                    ObstacleHeight = heightMetersFromInput,
                    ObstacleDescription = form["ObstacleDescription"],
                    Latitude = ParseDecimalRaw(form["Latitude"]),
                    Longitude = ParseDecimalRaw(form["Longitude"]),
                    GeometryJson = form["GeometryJson"],
                    ReportedAt = DateTime.UtcNow,
                    DateData = DateTime.UtcNow
                };

                // Attach reporter details if available
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

            // Set report status depending on draft flag
            obstacledata.Status = isDraft ? ReportStatus.NotApproved : ReportStatus.Pending;

            if (!isDraft)
            {
                // Manual validation of required fields when submitting (not draft)
                // Drafts bypass this strict validation
                if (string.IsNullOrWhiteSpace(obstacledata.ObstacleName))
                    ModelState.AddModelError(nameof(obstacledata.ObstacleName), "Name is required.");

                if (string.IsNullOrWhiteSpace(obstacledata.ObstacleDescription))
                    ModelState.AddModelError(nameof(obstacledata.ObstacleDescription), "Description is required.");

                if (!obstacledata.Latitude.HasValue || !obstacledata.Longitude.HasValue)
                    ModelState.AddModelError("Coordinates", "Coordinates are required when submitting.");

                if (!obstacledata.CategoryId.HasValue || obstacledata.CategoryId <= 0)
                    ModelState.AddModelError(nameof(obstacledata.CategoryId), "Category is required when submitting.");

                if (!obstacledata.ObstacleHeight.HasValue)
                    ModelState.AddModelError(nameof(obstacledata.ObstacleHeight), "Height is required when submitting.");

                if (!ModelState.IsValid)
                {
                    // If validation fails, categorize options and return to form view
                    ViewBag.CategoryOptions = _context.ObstacleCategories
                        .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = c.Name
                        })
                        .ToList();

                    return View(obstacledata);
                }
            }

            await _context.SaveChangesAsync();

            if (isDraft)
            {
                TempData["SuccessMessage"] = "Report saved as draft. You can edit it from 'My Reports'.";
            }

            return View("Overview", obstacledata);
        }

        // GET: /Obstacle/Review
        // Redirects to the Admin Dashboard
        [Authorize(Roles = "Registry Administrator")]
        [HttpGet]
        public IActionResult Review()
        {
            return RedirectToAction("Dashboard", "AdminObstacle");
        }

        // GET: /Obstacle/ReviewPending
        // Redirects to the Admin Dashboard filtered by Pending status
        [Authorize(Roles = "Registry Administrator")]
        [HttpGet]
        public IActionResult ReviewPending()
        {
            return RedirectToAction("Dashboard", "AdminObstacle", new { filterStatus = "Pending" });
        }

        // POST: /Obstacle/UpdateStatus
        // Updates status and redirects to the Admin Dashboard
        [Authorize(Roles = "Registry Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, ReportStatus status)
        {
            // Redirect to AdminObstacle Dashboard. Actual status update is handled there.
            return RedirectToAction("Dashboard", "AdminObstacle");
        }
    }
}

