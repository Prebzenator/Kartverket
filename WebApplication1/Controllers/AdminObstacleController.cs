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
    /// Registry Administrator controller for reviewing and managing obstacle reports.
    /// Only users in "Registry Administrator" role can access these actions.
    /// 
    /// DESIGN CHANGE: Now shows ALL reports including drafts (Status = NotApproved without admin comments).
    /// Previously, draft reports were filtered out from the admin dashboard.
    /// 
    /// Note: Admins typically don't process NotApproved reports (drafts or rejections),
    /// they focus on Pending reports. However, showing all reports provides full visibility.
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
        /// GET: /AdminObstacle/Dashboard
        /// 
        /// Main admin dashboard showing all reports with filtering and sorting capabilities.
        /// 
        /// IMPORTANT: Shows ALL reports including:
        /// - Drafts (NotApproved without admin comments)
        /// - Pending reports (awaiting review)
        /// - Approved reports
        /// - Rejected reports (NotApproved with admin comments)
        /// 
        /// Filtering options:
        /// - By status (all, Pending, Approved, NotApproved)
        /// - By organization (all, NLA, Luftforsvaret, etc.)
        /// 
        /// Sorting options:
        /// - Date (newest/oldest first)
        /// - Obstacle name
        /// - Height
        /// - Status
        /// - Reporter name
        /// - Organization
        /// 
        /// Note: Admins typically filter to Status=Pending to see only reports needing action.
        /// </summary>
        /// <param name="sortBy">Sort field (date, name, height, status, reporter, organization)</param>
        /// <param name="filterStatus">Status filter (all, Pending, Approved, NotApproved)</param>
        /// <param name="filterOrg">Organization filter (all, or specific organization name)</param>
        [HttpGet]
        public async Task<IActionResult> Dashboard(
    string sortBy = "date",
    string filterStatus = "all",
    string filterOrg = "all",
    string filterCategory = "all",
    string? q = null)
        {
            // Baseline query — include Category so navigation property is populated
            var query = _context.Obstacles
                .AsNoTracking()
                .Include(o => o.Category)
                .AsQueryable();

            // Optional search (obstacle name or reporter)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qTrim = q.Trim();
                query = query.Where(o => o.ObstacleName.Contains(qTrim) || (o.ReporterName != null && o.ReporterName.Contains(qTrim)));
            }

            // Apply status filter when requested
            if (filterStatus != "all" && Enum.TryParse<ReportStatus>(filterStatus, out var status))
            {
                query = query.Where(o => o.Status == status);
            }

            // Organization filter
            if (filterOrg != "all")
            {
                // This ensures we only get reports with a non-null organization matching the filter
                query = query.Where(o => o.ReporterOrganization != null && o.ReporterOrganization == filterOrg);
            }

            // Category filter: support "all", "uncategorized" (CategoryId == null) or categoryId as int
            if (!string.IsNullOrEmpty(filterCategory) && filterCategory != "all")
            {
                if (filterCategory == "uncategorized")
                {
                    query = query.Where(o => o.CategoryId == null);
                }
                else if (int.TryParse(filterCategory, out var catId))
                {
                    query = query.Where(o => o.CategoryId == catId);
                }
                // else: ignore invalid value defensively
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

            // Materialize results
            var reports = await query.ToListAsync();

            // DEBUG: counts of what query returned (use null checks for uncategorized)
            ViewBag.Debug_ReturnedCount = reports.Count;
            ViewBag.Debug_ReturnedUncategorized = reports.Count(r => r.CategoryId == null);

            // Get all Registry Administrators for assignment dropdown
            var admins = await _userManager.GetUsersInRoleAsync("Registry Administrator");

            // Get distinct organizations for filter dropdown
            var organizations = await _context.Obstacles
                .Where(o => o.ReporterOrganization != null)
                .Select(o => o.ReporterOrganization)
                .Distinct()
                .ToListAsync();

            // Categories for the filter dropdown (Id + Name) — DO NOT add synthetic Id=0 here
            var categoriesFromDb = await _context.ObstacleCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            // Baseline debug counters (database-wide)
            ViewBag.Debug_TotalInDb = await _context.Obstacles.CountAsync();
            ViewBag.Debug_UncategorizedInDb = await _context.Obstacles.CountAsync(o => o.CategoryId == null);

            // Build view model with all data needed for dashboard
            var viewModel = new AdminDashboardViewModel
            {
                Reports = reports,
                Administrators = admins.ToList(),
                Organizations = organizations,
                CurrentSort = sortBy,
                CurrentStatusFilter = filterStatus,
                CurrentOrgFilter = filterOrg,
                CurrentCategoryFilter = filterCategory,
                Query = q
            };

            // Send categories and admins to view for dropdowns (no synthetic 'Uncategorized' entry)
            ViewBag.Categories = categoriesFromDb;
            ViewBag.Administrators = admins;

            return View(viewModel);
        }


        [HttpGet]
        public async Task<IActionResult> ViewReport(int id)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null)
                return NotFound();

            // Get all Registry Administrators for assignment
            var admins = await _userManager.GetUsersInRoleAsync("Registry Administrator");

            ViewBag.Administrators = admins;
            return View(report);
        }

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
                report.AssignedToUserId = null;
                report.AssignedToName = null;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Report assignment updated.";
            return RedirectToAction(nameof(ViewReport), new { id });
        }

        /// <summary>
        /// POST: /AdminObstacle/ApproveReport
        /// 
        /// Approves a report, marking it ready for integration into NRL.
        /// 
        /// What happens when approving:
        /// 1. Status changed to Approved
        /// 2. ReviewedByUserId and ReviewedByName set to current admin
        /// 3. LastReviewedAt timestamp set to now
        /// 4. AdminComments cleared (approval means no issues to report)
        /// 
        /// Approved reports are considered validated and ready for the National
        /// Aviation Obstacle Register (NRL) integration.
        /// 
        /// The pilot who submitted the report will see their report is approved.
        /// </summary>
        /// <param name="id">Report ID to approve</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReport(int id)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;
            var currentUserName = currentUser?.FullName ?? currentUser?.Email ?? currentUser?.UserName ?? "Unknown";

            // Update status to Approved
            report.Status = ReportStatus.Approved;

            // Record who approved it and when (audit trail)
            report.ReviewedByUserId = currentUserId;
            report.ReviewedByName = currentUserName;
            report.LastReviewedAt = DateTime.UtcNow;

            // Clear any previous rejection comments (approval = no issues)
            report.AdminComments = null;

            // Assign to the approver
            report.AssignedToUserId = currentUserId;
            report.AssignedToName = currentUserName;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Report #{id} approved and assigned to {currentUserName}.";
            return RedirectToAction(nameof(Dashboard));
        }


        /// <summary>
        /// POST: /AdminObstacle/RejectReport
        /// 
        /// Rejects a report with mandatory feedback explaining why.
        /// 
        /// What happens when rejecting:
        /// 1. Status changed to NotApproved (same as draft, but has admin comments)
        /// 2. AdminComments set to rejection reason (MANDATORY)
        /// 3. ReviewedByUserId and ReviewedByName set to current admin
        /// 4. LastReviewedAt timestamp set to now
        /// 
        /// The pilot will see:
        /// - Report status as "Rejected"
        /// - Admin feedback explaining why (visible in their report view)
        /// 
        /// This allows the pilot to understand the issue and potentially
        /// correct and resubmit the report.
        /// 
        /// IMPORTANT: AdminComments are mandatory for rejection to ensure
        /// pilots understand what needs to be corrected.
        /// </summary>
        /// <param name="id">Report ID to reject</param>
        /// <param name="adminComments">Rejection reason (mandatory)</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReport(int id, string adminComments)
        {
            // Validate that comments are provided
            if (string.IsNullOrWhiteSpace(adminComments))
            {
                TempData["ErrorMessage"] = "Comments are required when rejecting a report.";
                return RedirectToAction(nameof(ViewReport), new { id });
            }

            var report = await _context.Obstacles.FindAsync(id);
            if (report == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            // Update status to NotApproved (rejection)
            // Note: NotApproved with comments = rejection, without comments = draft
            report.Status = ReportStatus.NotApproved;
            report.AdminComments = adminComments; // This distinguishes rejection from draft

            // Record who rejected it and when (audit trail)
            report.ReviewedByUserId = currentUser?.Id;
            report.ReviewedByName = currentUser?.FullName ?? currentUser?.Email;
            report.LastReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Report rejected with feedback.";
            return RedirectToAction(nameof(Dashboard));
        }

        /// <summary>
        /// POST: /AdminObstacle/SetPending
        /// 
        /// Sets a report back to Pending status.
        /// 
        /// Use cases:
        /// - Admin accidentally approved/rejected and wants to undo
        /// - Report needs re-review after pilot made corrections
        /// - Reassigning report to another admin for fresh review
        /// 
        /// What happens:
        /// 1. Status changed to Pending
        /// 2. ReviewedByUserId and ReviewedByName updated to current admin
        /// 3. LastReviewedAt timestamp updated
        /// 4. AdminComments preserved (if any exist from previous rejection)
        /// 
        /// Note: This does NOT clear admin comments, allowing admins to see
        /// previous feedback history when re-reviewing.
        /// </summary>
        /// <param name="id">Report ID to set as pending</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPending(int id)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            // Set back to Pending status (awaiting review)
            report.Status = ReportStatus.Pending;

            // Update review tracking (who and when it was set to pending)
            report.ReviewedByUserId = currentUser?.Id;
            report.ReviewedByName = currentUser?.FullName ?? currentUser?.Email;
            report.LastReviewedAt = DateTime.UtcNow;

            // Note: AdminComments are NOT cleared - preserved for context

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Report status set to Pending.";
            return RedirectToAction(nameof(ViewReport), new { id });
        }
    }
}