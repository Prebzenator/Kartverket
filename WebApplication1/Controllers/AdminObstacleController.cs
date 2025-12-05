using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Controller for Registry Administrators to review, approve, or reject obstacle reports.
    /// Manage obstacle reports submitted by users.
    /// Only registry administrators have access.
    /// </summary>
    [Authorize(Roles = "Registry Administrator")]
    public class AdminObstacleController : Controller
    {
        private readonly ApplicationDbContext _context; // Database context
        private readonly UserManager<ApplicationUser> _userManager; // User manager for identity operations

// inject required services for database access and user management
        public AdminObstacleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

// GET: /AdminObstacle/Dashboard
// Displays the main dashboard with filtering and sorting options for all reports
// Administrators can filter by status, organization, category, and search text
    
        [HttpGet]
        public async Task<IActionResult> Dashboard(
            string sortBy = "date",
            string filterStatus = "all",
            string filterOrg = "all",
            string filterCategory = "all",
            string? q = null)
        {
// Base query for obstacles 
            var query = _context.Obstacles
                .AsNoTracking()
                .Include(o => o.Category)
                .AsQueryable();

//Search by name or reporter name
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qTrim = q.Trim();
                query = query.Where(o => o.ObstacleName.Contains(qTrim) || (o.ReporterName != null && o.ReporterName.Contains(qTrim)));
            }
//
            if (filterStatus != "all" && Enum.TryParse<ReportStatus>(filterStatus, out var status))
            {
                query = query.Where(o => o.Status == status);
            }
// Filter by reporting organization
            if (filterOrg != "all")
            {
                query = query.Where(o => o.ReporterOrganization != null && o.ReporterOrganization == filterOrg);
            }
// Filter by category
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
            }
// Sorting option
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
// Debug info for admin

            ViewBag.Debug_ReturnedCount = reports.Count;
            ViewBag.Debug_ReturnedUncategorized = reports.Count(r => r.CategoryId == null);

// Load administrators for assignment
            var admins = await _userManager.GetUsersInRoleAsync("Registry Administrator");

// Distinct list of orgnaizations for filtering
            var organizations = await _context.Obstacles
                .Where(o => o.ReporterOrganization != null)
                .Select(o => o.ReporterOrganization)
                .Distinct()
                .ToListAsync();
// Category list for dropdowns
            var categoriesFromDb = await _context.ObstacleCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            ViewBag.Debug_TotalInDb = await _context.Obstacles.CountAsync();
            ViewBag.Debug_UncategorizedInDb = await _context.Obstacles.CountAsync(o => o.CategoryId == null);

// Build view model
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

            ViewBag.Categories = categoriesFromDb;
            ViewBag.Administrators = admins;

            return View(viewModel);
        }

// GET: /AdminObstacle/ViewReport/{id}
// Loads a single report by id
// Load all registry administrators for assignment
// Display the report details view
        [HttpGet]
        public async Task<IActionResult> ViewReport(int id)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            var admins = await _userManager.GetUsersInRoleAsync("Registry Administrator");
            ViewBag.Administrators = admins;

            return View(report);
        }

// POST: /AdminObstacle/UpdateReport/{id}
// Updates report details such as name, height, description, and location
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReport(int id, ObstacleData model)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            report.ObstacleName = model.ObstacleName;
            report.ObstacleHeight = model.ObstacleHeight;
            report.ObstacleDescription = model.ObstacleDescription;
            report.Latitude = model.Latitude;
            report.Longitude = model.Longitude;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Report updated successfully.";
            return RedirectToAction(nameof(ViewReport), new { id });
        }

// POST: /AdminObstacle/AssignReport
// Assign or unassign a report to a registry administrator
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReport(int id, string assignToUserId)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

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

        

// POST: /AdminObstacle/ApproveReport
 // Marks a report as Approved, indicating it is ready for integration into the national register.
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReport(int id)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;
            var currentUserName = currentUser?.FullName ?? currentUser?.Email ?? currentUser?.UserName ?? "Unknown";

            report.Status = ReportStatus.Approved;
            report.ReviewedByUserId = currentUserId;
            report.ReviewedByName = currentUserName;
            report.LastReviewedAt = DateTime.UtcNow;
            report.AdminComments = null; // Clear rejection comments

            report.AssignedToUserId = currentUserId;
            report.AssignedToName = currentUserName;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Report #{id} approved and assigned to {currentUserName}.";
            return RedirectToAction(nameof(Dashboard));
        }


// POST: /AdminObstacle/RejectReport
// Rejects a report and requires administrative comments explaining the reason.
        
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
            if (report == null) return NotFound();

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

// POST: /AdminObstacle/SetPending
// <summary>
// Reverts a report status to Pending.
// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPending(int id)
        {
            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

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