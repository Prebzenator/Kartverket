using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Data;
using System.Linq;
using System.Threading.Tasks;
using System;

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

            return View(model);
        }


        /// <summary>
        /// Handles form submission, validates input, saves to database, and shows overview.
        /// Supports both create and update.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataForm(IFormCollection form, string? IsDraft, int? id)
        {
            var isDraft = string.Equals(IsDraft, "true", StringComparison.OrdinalIgnoreCase);
            var user = await _userManager.GetUserAsync(User);

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
                obstacledata.ObstacleHeight = decimal.TryParse(form["ObstacleHeight"], out var h) ? h : obstacledata.ObstacleHeight;
                obstacledata.ObstacleDescription = form["ObstacleDescription"];
                obstacledata.Latitude = decimal.TryParse(form["Latitude"], out var lat) ? lat : obstacledata.Latitude;
                obstacledata.Longitude = decimal.TryParse(form["Longitude"], out var lng) ? lng : obstacledata.Longitude;
                obstacledata.ReportedAt = DateTime.UtcNow;

                // ✅ Update category if provided
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

                // ✅ Set category when creating
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

            // ✅ Load Category navigation property before showing overview
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
 