<<<<<<< HEAD
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
=======
﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
>>>>>>> backup-log
using WebApplication1.Models;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Handles routes for obstacle data input and overview display (Obstacle registration).
    /// 
    /// Routes:
    /// - GET /Obstacle/DataForm: Displays the form for entering obstacle data.
    /// - POST /Obstacle/DataForm: Handles form submission, validates input, saves to database, and shows overview.
    /// - GET /Obstacle/Overview: Displays the overview of submitted obstacle data.
    /// 
    /// Notes:
    /// - Uses Entity Framework Core via ApplicationDbContext to persist obstacle reports.
    /// - Model validation is enforced via data annotations in ObstacleData.cs.
    /// - LoggedAt timestamp is automatically set at object creation.
    /// - User info is automatically captured from the logged-in user.
    /// </summary>
    [Authorize] // Require login to submit obstacles
    public class ObstacleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Initializes the controller with the application's database context and user manager.
        /// </summary>
        /// <param name="context">Injected EF Core database context.</param>
        /// <param name="userManager">Injected Identity UserManager for accessing user data.</param>
        public ObstacleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Displays the form for entering obstacle data (GET).
        /// </summary>
        /// <returns>View with empty ObstacleData model.</returns>
        [HttpGet]
        public ActionResult DataForm()
        {
            return View();
        }

        /// <summary>
        /// Handles the form submission for obstacle data (POST).
        /// - Validates the model.
        /// - Captures the logged-in user's information.
        /// - Saves the report to the database if valid.
        /// - Displays the overview page with submitted data.
        /// </summary>
        /// <param name="obstacledata">User-submitted obstacle data.</param>
        /// <returns>Overview view if valid; otherwise redisplays form with validation errors.</returns>
        [HttpPost]
        public async Task<IActionResult> DataForm(ObstacleData obstacledata)
        {
            if (!ModelState.IsValid)
            {
                return View(obstacledata);
            }

<<<<<<< HEAD
            // Get the currently logged-in user
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                // Store who submitted this obstacle
                obstacledata.ReportedByUserId = user.Id;
                obstacledata.ReporterName = user.FullName;
                obstacledata.ReporterOrganization = user.Organization;
            }

            // Set default status for new reports
            obstacledata.Status = "Pending";
=======
            // >>> MOVED INSIDE METHOD (before Add/Save) <<<
            /// If the user is authenticated, capture who submitted the report.
            /// Username is taken from the identity; email can be captured when Identity is wired up.
            if (User?.Identity?.IsAuthenticated == true)
            {
                obstacledata.SubmittedByUserName = User.Identity!.Name;
                // If/when ASP.NET Core Identity is configured with emails, set it like this:
                // obstacledata.SubmittedByEmail = (await _userManager.GetUserAsync(User))?.Email;
            }
            // If you also allow anonymous submissions with an optional email field in the form,
            // you can map that here later (e.g., obstacledata.SubmittedByEmail = postedEmail).
>>>>>>> backup-log

            _context.Obstacles.Add(obstacledata);
            await _context.SaveChangesAsync();

            return View("Overview", obstacledata);
        }

        /// <summary>
        /// Updates the approval status for an obstacle report.
        /// Called when admin clicks approve/reject buttons on overview page.
        /// </summary>
        /// <param name="id">ID of the obstacle report to update.</param>
        /// <param name="status">New status to set (Approved or Rejected).</param>
        /// <returns>Redirects back to the previous page or Index if not available.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ReportStatus status)
        {
            var entity = await _context.Obstacles.FirstOrDefaultAsync(o => o.Id == id);
            if (entity == null)
                return NotFound();

            entity.Status = status;
            await _context.SaveChangesAsync();

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrWhiteSpace(referer))
                return Redirect(referer);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Displays all obstacle reports for admin review.
        /// Allows administrators to approve or reject submitted reports.
        /// </summary>
        /// <returns>View with a list of all obstacle reports sorted by newest first.</returns>
        [HttpGet]
        public async Task<IActionResult> Review()
        {
            var items = await _context.Obstacles
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(items);
        }

        /// <summary>
        /// Displays only reports that are still pending approval.
        /// Used by administrators to quickly find unreviewed submissions.
        /// </summary>
        /// <returns>View showing only pending obstacle reports.</returns>
        [HttpGet]
        public async Task<IActionResult> ReviewPending()
        {
            var items = await _context.Obstacles
                .Where(o => o.Status == ReportStatus.Pending)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View("Review", items);
        }
    }
}