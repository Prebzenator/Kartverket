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

        /// <summary>
        /// Initializes the PilotController with database context and user manager.
        /// </summary>
        public PilotController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Pilot/Log
        // Displays a dashboard of all reports associated with the user's organization
        // Allows organization members to coordinate by viewing colleague reports
        [HttpGet]
        public async Task<IActionResult> Log()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userOrg = user.Organization;

            if (string.IsNullOrEmpty(userOrg))
            {
                // Safety check:
                // User has no organization assigned, cannot fetch reports
                TempData["ErrorMessage"] = "You don't have an organization assigned.";
                return View(new List<ObstacleData>());
            }

            // Retrieve all reports from the user's organization, ordered by most recent first
            var reports = await _context.Obstacles
                .Where(o => o.ReporterOrganization == userOrg)
                .OrderByDescending(o => o.ReportedAt)
                .ToListAsync();

            // Pass current user and organization info to the view
            ViewBag.CurrentUserId = user.Id;
            ViewBag.OrganizationName = userOrg;

            return View(reports);
        }

        // GET: /Pilot/EditReport/{id}
        // Provides the form to edit a report owned by the current user
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

            // Prevent editing if report is already approved or rejected
            if (report.Status == ReportStatus.Approved || report.Status == ReportStatus.NotApproved)
            {
                TempData["ErrorMessage"] = "This report has already been processed by admin and cannot be edited.";
                return RedirectToAction("ViewReport", new { id = report.Id });
            }

            // Show category options in the view
            ViewBag.CategoryOptions = _context.ObstacleCategories
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

            // Reuse the obstacle DataForm view for editing
            return View("~/Views/Obstacle/DataForm.cshtml", report);
        }

        // POST: /Pilot/EditReport/{id}
        // Handles submission of edited report data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReport(int id, ObstacleData updatedReport)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            // Security check: Users can only edit their own reports
            if (report.ReportedByUserId != user.Id)
                return Forbid();

            // Prevent editing if report is already approved or rejected
            if (report.Status == ReportStatus.Approved || report.Status == ReportStatus.NotApproved)
            {
                TempData["ErrorMessage"] = "This report has already been processed by admin and cannot be edited.";
                return RedirectToAction("ViewReport", new { id = report.Id });
            }

            // Update allowed fields only (ownership and status already checked)
            report.ObstacleName = updatedReport.ObstacleName;
            report.ObstacleDescription = updatedReport.ObstacleDescription;
            report.ObstacleHeight = updatedReport.ObstacleHeight;
            report.Latitude = updatedReport.Latitude;
            report.Longitude = updatedReport.Longitude;
            report.CategoryId = updatedReport.CategoryId;
            report.GeometryJson = updatedReport.GeometryJson;

            await _context.SaveChangesAsync();

            // Redirect back to the detailed view of the updated report
            return RedirectToAction("ViewReport", new { id = report.Id });
        }

        // GET: /Pilot/ViewReport/{id}
        // Displays detailed information for a specific report
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

            // Only allow edit if user owns the report AND it is still pending
            ViewBag.CanEdit = (report.ReportedByUserId == user.Id && report.Status == ReportStatus.Pending);

            return View(report);
        }
    }
}
