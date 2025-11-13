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
        /// Handles form submission, validates input, saves to database, and shows overview.
        /// Supports both create and update.
        ///
        /// Behavior around height:
        /// - The form field used by the view is "HeightInputRaw".
        /// - Pilots enter heights in feet; controller converts to meters before saving.
        /// - Non-pilots enter heights in meters directly.
        /// - Database (ObstacleHeight) remains canonical in meters.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataForm(Microsoft.AspNetCore.Http.IFormCollection form, string? IsDraft, int? id)
        {
            var isDraft = string.Equals(IsDraft, "true", StringComparison.OrdinalIgnoreCase);
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

            ObstacleData obstacledata;
            if (id.HasValue && id.Value > 0)
            {
                // Update existing obstacle
                obstacledata = await _context.Obstacles.FindAsync(id.Value);
                if (obstacledata == null) return NotFound();

                if (user != null && obstacledata.ReporterOrganization != user.Organization)
                {
                    return Forbid();
                }

                obstacledata.ObstacleName = form["ObstacleName"];

                // If user provided a height input we use the parsed/converted value, otherwise keep existing
                obstacledata.ObstacleHeight = heightMetersFromInput ?? obstacledata.ObstacleHeight;

                obstacledata.ObstacleDescription = form["ObstacleDescription"];
                obstacledata.Latitude = ParseDecimalRaw(form["Latitude"]) ?? obstacledata.Latitude;
                obstacledata.Longitude = ParseDecimalRaw(form["Longitude"]) ?? obstacledata.Longitude;
                obstacledata.ReportedAt = DateTime.UtcNow;

                // Update category if provided
                if (int.TryParse(form["CategoryId"], out var categoryId))
                {
                    obstacledata.CategoryId = categoryId;
                }
            }
            else
            {
                // Create new obstacle
                obstacledata = new ObstacleData
                {
                    ObstacleName = form["ObstacleName"],
                    // Use converted meters (may be null if no input)
                    ObstacleHeight = heightMetersFromInput,
                    ObstacleDescription = form["ObstacleDescription"],
                    Latitude = ParseDecimalRaw(form["Latitude"]),
                    Longitude = ParseDecimalRaw(form["Longitude"]),
                    ReportedAt = DateTime.UtcNow,
                    DateData = DateTime.UtcNow
                };

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

            obstacledata.Status = isDraft ? ReportStatus.NotApproved : ReportStatus.Pending;

            // Validate if not draft
            if (!isDraft && !TryValidateModel(obstacledata))
            {
                // Repopulate category options if validation fails
                ViewBag.CategoryOptions = _context.ObstacleCategories
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToList();

                return View(obstacledata);
            }

            await _context.SaveChangesAsync();

            // Load Category navigation property before showing overview
            await _context.Entry(obstacledata).Reference(o => o.Category).LoadAsync();

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
                .Include(o => o.Category)
                .OrderByDescending(o => o.ReportedAt)
                .ToList();

            return View(allObstacles);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult ReviewPending()
        {
            var pendingObstacles = _context.Obstacles
                .Where(o => o.Status == ReportStatus.Pending)
                .Include(o => o.Category)
                .OrderByDescending(o => o.ReportedAt)
                .ToList();

            return View("Review", pendingObstacles);
        }

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
