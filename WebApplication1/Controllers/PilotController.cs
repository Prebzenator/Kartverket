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

        // GET: /Pilot/Log
        [HttpGet]
        public async Task<IActionResult> Log()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var org = user.Organization ?? string.Empty;

            var reports = await _context.Obstacles
                .Where(o => o.ReporterOrganization == org)
                .OrderByDescending(o => o.ReportedAt)
                .ToListAsync();

            return View(reports);
        }

        // GET: /Pilot/EditReport/{id}
        [HttpGet]
        public async Task<IActionResult> EditReport(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var report = await _context.Obstacles.FindAsync(id);
            if (report == null) return NotFound();

            if (report.ReporterOrganization != user.Organization)
            {
                return Forbid();
            }

            // Reuse existing DataForm view for editing
            return View("~/Views/Obstacle/DataForm.cshtml", report);
        }
    }
}
