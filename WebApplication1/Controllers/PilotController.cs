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
    /// Pilot-specific controller for viewing and managing reports.
    /// 
    /// DESIGN CHANGE: This controller now shows ALL reports from the user's organization,
    /// not just the user's own reports. This allows crew members to see what their colleagues
    /// have reported, enabling internal coordination before obstacles are registered in NRL.
    /// 
    /// Pilots can:
    /// - View all reports from their organization
    /// - Edit any of their own reports (drafts or submitted)
    /// - See status and admin feedback on their reports
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
        /// 
        /// Main pilot dashboard showing all reports from the user's organization.
        /// 
        /// IMPORTANT: This shows organization-wide reports, not just user's own reports.
        /// - User's own reports are highlighted with a blue border and star icon
        /// - Other organization members' reports are shown for visibility
        /// - Edit buttons only appear for user's own reports
        /// 
        /// Organization filtering ensures:
        /// - NLA pilots see all NLA reports
        /// - Luftforsvaret pilots see all Luftforsvaret reports
        /// - etc.
        /// 
        /// This supports the requirement: "organisasjon, for eksempel NLA eller Luftforsvaret 
        /// bør ha mulighet til å se hvilke innrapporteringer deres besetningsmedlemmer har gjort"
        /// (organizations should be able to see what their crew members have reported)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Log()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userOrg = user.Organization;

            // Check if user has an organization assigned
            if (string.IsNullOrEmpty(userOrg))
            {
                TempData["ErrorMessage"] = "You don't have an organization assigned.";
                return View(new List<ObstacleData>());
            }

            // Get ALL reports from the same organization (not just user's reports)
            // Includes all statuses: drafts (NotApproved), pending, approved, and rejected
            // Ordered by most recent first for better UX
            var reports = await _context.Obstacles
                .Where(o => o.ReporterOrganization == userOrg)
                .OrderByDescending(o => o.ReportedAt)
                .ToListAsync();

            // Pass current user ID to view so it can highlight user's own reports
            ViewBag.CurrentUserId = user.Id;
            ViewBag.OrganizationName = userOrg;

            return View(reports);
        }

        /// <summary>
        /// GET: /Pilot/EditReport/{id}
        /// 
        /// Allows editing of any report owned by the user.
        /// 
        /// DESIGN CHANGE: Previously only draft reports could be edited.
        /// Now pilots can edit ANY of their reports regardless of status.
        /// This allows pilots to:
        /// - Update incomplete information in draft reports
        /// - Correct mistakes in submitted (Pending) reports
        /// - Fix errors in approved reports
        /// - Respond to rejection feedback by fixing and resubmitting
        /// 
        /// Security:
        /// - Users can only edit their own reports (ReportedByUserId check)
        /// - Users cannot edit other organization members' reports
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditReport(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            // Security check: User must own the report
            // This prevents users from editing reports submitted by their colleagues
            if (report.ReportedByUserId != user.Id)
            {
                return Forbid(); // Return 403 Forbidden
            }

            // ✅ Legg til kategori-valgene for dropdown
            ViewBag.CategoryOptions = _context.ObstacleCategories
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

            // Reuse existing DataForm view for editing
            // The view detects editing mode by checking if model.Id > 0
            return View("~/Views/Obstacle/DataForm.cshtml", report);
        }

        /// <summary>
        /// GET: /Pilot/ViewReport/{id}
        /// 
        /// View detailed information about a specific report.
        /// Shows full report details including status, admin feedback, and coordinates.
        /// 
        /// Security:
        /// - Users can view their own reports (full access)
        /// - Users can view other organization members' reports (read-only via modal in Log view)
        /// - Users cannot view reports from other organizations
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ViewReport(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            // Security check: User must own the report OR be from same organization
            // This allows organization members to view each other's reports for coordination
            if (report.ReportedByUserId != user.Id && report.ReporterOrganization != user.Organization)
            {
                return Forbid(); // Return 403 Forbidden if not same organization
            }

            // Flag if user can edit this report (only own reports)
            // Used in view to show/hide Edit button
            ViewBag.CanEdit = (report.ReportedByUserId == user.Id);

            return View(report);
        }
    }
}