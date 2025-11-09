using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Data;
using WebApplication1.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Admin-only controller for reviewing and managing obstacle reports.
    /// Only users in "Registry Administrator" role can access these actions.
    /// </summary>
    [Authorize(Roles = "Registry Administrator")]
    public class AdminObstacleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminObstacleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Main admin dashboard - shows all reports with filtering and sorting.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Dashboard(string sortBy = "date", string filterStatus = "all", string filterOrg = "all")
        {
            // Get all reports
            var query = _context.Obstacles.AsQueryable();

            // Apply status filter
            if (filterStatus != "all" && Enum.TryParse<ReportStatus>(filterStatus, out var status))
            {
                query = query.Where(o => o.Status == status);
            }

            // Apply organization filter
            if (filterOrg != "all")
            {
                query = query.Where(o => o.ReporterOrganization == filterOrg);
            }

            // Apply sorting
            query = sortBy switch
            {
                "date" => query.OrderByDescending(o => o.ReportedAt),
                "date-asc" => query.OrderBy(o => o.ReportedAt),
                "name" => query.OrderBy(o => o.ObstacleName),
                "height" => query.OrderByDescending(o => o.ObstacleHeight),
                "status" => query.OrderBy(o => o.Status),
                "reporter" => query.OrderBy(o => o.ReporterName),
                "organization" => query.OrderBy(o => o.ReporterOrganization),
                _ => query.OrderByDescending(o => o.ReportedAt)
            };

            var reports = await query.ToListAsync();

            // Get all admins for assignment dropdown
            var admins = await _userManager.GetUsersInRoleAsync("Registry Administrator");

            // Get distinct organizations for filter
            var organizations = await _context.Obstacles
                .Where(o => o.ReporterOrganization != null)
                .Select(o => o.ReporterOrganization)
                .Distinct()
                .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                Reports = reports,
                Administrators = admins.ToList(),
                Organizations = organizations,
                CurrentSort = sortBy,
                CurrentStatusFilter = filterStatus,
                CurrentOrgFilter = filterOrg
            };

            return View(viewModel);
        }

        /// <summary>
        /// View detailed information about a specific report.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ViewReport(int id)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null)
                return NotFound();

            // Get all admins for assignment
            var admins = await _userManager.GetUsersInRoleAsync("Registry Administrator");

            ViewBag.Administrators = admins;
            return View(report);
        }

        /// <summary>
        /// Edit report details (admin can update obstacle information).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReport(int id, ObstacleData model)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null)
                return NotFound();

            // Update editable fields
            report.ObstacleName = model.ObstacleName;
            report.ObstacleHeight = model.ObstacleHeight;
            report.ObstacleDescription = model.ObstacleDescription;
            report.Latitude = model.Latitude;
            report.Longitude = model.Longitude;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Report updated successfully.";
            return RedirectToAction(nameof(ViewReport), new { id });
        }

        /// <summary>
        /// Assign a report to another administrator.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReport(int id, string assignToUserId)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null)
                return NotFound();

            if (!string.IsNullOrEmpty(assignToUserId))
            {
                var assignee = await _userManager.FindByIdAsync(assignToUserId);
                if (assignee != null)
                {
                    report.AssignedToUserId = assignee.Id;
                    report.AssignedToName = assignee.FullName ?? assignee.Email;
                }
            }
            else
            {
                // Unassign
                report.AssignedToUserId = null;
                report.AssignedToName = null;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Report assignment updated.";
            return RedirectToAction(nameof(ViewReport), new { id });
        }

        /// <summary>
        /// Approve a report (sets status to Approved).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReport(int id)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            report.Status = ReportStatus.Approved;
            report.ReviewedByUserId = currentUser?.Id;
            report.ReviewedByName = currentUser?.FullName ?? currentUser?.Email;
            report.LastReviewedAt = DateTime.UtcNow;
            report.AdminComments = null; // Clear comments on approval

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Report approved successfully.";
            return RedirectToAction(nameof(Dashboard));
        }

        /// <summary>
        /// Reject a report with mandatory comments.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReport(int id, string adminComments)
        {
            if (string.IsNullOrWhiteSpace(adminComments))
            {
                TempData["ErrorMessage"] = "Comments are required when rejecting a report.";
                return RedirectToAction(nameof(ViewReport), new { id });
            }

            var report = await _context.Obstacles.FindAsync(id);
            if (report == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            report.Status = ReportStatus.NotApproved;
            report.AdminComments = adminComments;
            report.ReviewedByUserId = currentUser?.Id;
            report.ReviewedByName = currentUser?.FullName ?? currentUser?.Email;
            report.LastReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Report rejected with feedback.";
            return RedirectToAction(nameof(Dashboard));
        }

        /// <summary>
        /// Set report status back to Pending.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPending(int id)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            report.Status = ReportStatus.Pending;
            report.ReviewedByUserId = currentUser?.Id;
            report.ReviewedByName = currentUser?.FullName ?? currentUser?.Email;
            report.LastReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Report status set to Pending.";
            return RedirectToAction(nameof(ViewReport), new { id });
        }
    }
}