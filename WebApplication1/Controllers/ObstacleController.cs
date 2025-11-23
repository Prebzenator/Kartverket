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
        /// Constructor injecting database context and user manager.
        /// </summary>
        public ObstacleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Displays the obstacle reporting form.
        /// </summary>
        /// <param name="missing">If true, prefills the name for a missing obstacle report.</param>
        [HttpGet]
        public IActionResult DataForm(bool missing = false)
        {
            var model = new ObstacleData();

            if (missing)
            {
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
        /// Processes the submission of an obstacle report (create or edit).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataForm(Microsoft.AspNetCore.Http.IFormCollection form, string? IsDraft, int? id)
        {
            var isDraft = string.Equals(IsDraft, "true", StringComparison.OrdinalIgnoreCase);
            var user = await _userManager.GetUserAsync(User);

            decimal? ParseDecimalRaw(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                raw = raw.Trim();
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv)) return inv;
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out var cur)) return cur;
                return null;
            }

            var heightRaw = form["HeightInputRaw"].FirstOrDefault();
            var parsedHeightInput = ParseDecimalRaw(heightRaw);

            decimal? heightMetersFromInput = null;
            if (parsedHeightInput.HasValue)
            {
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
                obstacledata = await _context.Obstacles.FindAsync(id.Value);
                if (obstacledata == null) return NotFound();

                if (user != null && obstacledata.ReportedByUserId != user.Id)
                {
                    return Forbid();
                }

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
                obstacledata = new ObstacleData
                {
                    ObstacleName = form["ObstacleName"].FirstOrDefault() ?? string.Empty,
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
        /// Redirects to the Admin Dashboard.
        /// </summary>
        [Authorize(Roles = "Registry Administrator")]
        [HttpGet]
        public IActionResult Review()
        {
            return RedirectToAction("Dashboard", "AdminObstacle");
        }

        /// <summary>
        /// Redirects to the Admin Dashboard filtered by Pending status.
        /// </summary>
        [Authorize(Roles = "Registry Administrator")]
        [HttpGet]
        public IActionResult ReviewPending()
        {
            return RedirectToAction("Dashboard", "AdminObstacle", new { filterStatus = "Pending" });
        }

        /// <summary>
        /// Redirects to the Admin Dashboard.
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
