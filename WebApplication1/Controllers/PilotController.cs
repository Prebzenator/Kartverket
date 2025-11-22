using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Controller for pilots to view organization reports and manage their own submissions.
    /// </summary>
    [Authorize(Roles = "Pilot")]
    public class PilotController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PilotController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Displays a dashboard of all reports associated with the user's organization.
        /// Allows organization members to coordinate by viewing colleague reports.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Log()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userOrg = user.Organization;

            if (string.IsNullOrEmpty(userOrg))
            {
                TempData["ErrorMessage"] = "You don't have an organization assigned.";
                return View(new List<ObstacleData>());
            }

            // Retrieve all reports from the user's organization, ordered by recency
            var reports = await _context.Obstacles
                .Where(o => o.ReporterOrganization == userOrg)
                .OrderByDescending(o => o.ReportedAt)
                .ToListAsync();

            ViewBag.CurrentUserId = user.Id;
            ViewBag.OrganizationName = userOrg;

            return View(reports);
        }

        /// <summary>
        /// Provides the form to edit a report owned by the current user.
        /// </summary>
        /// <param name="id">The ID of the report to edit.</param>
        [HttpGet]
        public async Task<IActionResult> EditReport(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            // Security check: Users can only edit their own reports
            if (report.ReportedByUserId != user.Id)
            {
                return Forbid();
            }

            ViewBag.CategoryOptions = _context.ObstacleCategories
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

            return View("~/Views/Obstacle/DataForm.cshtml", report);
        }

        /// <summary>
        /// Displays detailed information for a specific report.
        /// </summary>
        /// <param name="id">The ID of the report to view.</param>
        [HttpGet]
        public async Task<IActionResult> ViewReport(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            // Security check: Allow access if the user owns the report or belongs to the same organization
            if (report.ReportedByUserId != user.Id && report.ReporterOrganization != user.Organization)
            {
                return Forbid();
            }

            ViewBag.CanEdit = (report.ReportedByUserId == user.Id);

            return View(report);
        }
    }
}