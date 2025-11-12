using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Pilot-specific controller for viewing and managing own reports.
    /// Pilots can:
    /// - View all their reports (drafts, pending, approved, rejected)
    /// - See status and admin feedback
    /// - Edit draft reports only
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
        /// GET: /Pilot/Log
        /// Displays all reports submitted by the current user.
        /// Shows status, feedback, and edit options for drafts.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Log()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Get all reports by this user, ordered by most recent first
            var reports = await _context.Obstacles
                .Where(o => o.ReportedByUserId == user.Id)
                .OrderByDescending(o => o.ReportedAt)
                .ToListAsync();

            return View(reports);
        }

        /// <summary>
        /// GET: /Pilot/EditReport/{id}
        /// Allows editing of draft reports only.
        /// Returns 403 Forbidden if user doesn't own the report.
        /// Returns error message if report is not a draft.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditReport(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            // Security check: User must own the report
            if (report.ReportedByUserId != user.Id)
            {
                return Forbid();
            }

            // Only allow editing drafts
            if (!report.IsDraft)
            {
                TempData["ErrorMessage"] = "Cannot edit a report that has been submitted for review.";
                return RedirectToAction(nameof(Log));
            }

            // Reuse existing DataForm view for editing
            return View("~/Views/Obstacle/DataForm.cshtml", report);
        }

        /// <summary>
        /// GET: /Pilot/ViewReport/{id}
        /// View detailed information about a specific report.
        /// Shows status, feedback from admin, and allows editing if draft.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ViewReport(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            // Security check: User must own the report
            if (report.ReportedByUserId != user.Id)
            {
                return Forbid();
            }

            return View(report);
        }

        /// <summary>
        /// GET: /Pilot/OrganizationReports
        /// Shows all reports from the user's organization (not just their own).
        /// This allows organizations like NLA to see what their crew members have reported.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OrganizationReports()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Get user's organization
            var userOrg = user.Organization;

            if (string.IsNullOrEmpty(userOrg))
            {
                TempData["ErrorMessage"] = "You don't have an organization assigned.";
                return RedirectToAction(nameof(Log));
            }

            // Get all submitted reports (not drafts) from the same organization
            var reports = await _context.Obstacles
                .Where(o => !o.IsDraft && o.ReporterOrganization == userOrg)
                .OrderByDescending(o => o.ReportedAt)
                .ToListAsync();

            // Pass organization name to view
            ViewBag.OrganizationName = userOrg;
            ViewBag.CurrentUserId = user.Id; // To highlight user's own reports

            return View(reports);
        }
    }
}

