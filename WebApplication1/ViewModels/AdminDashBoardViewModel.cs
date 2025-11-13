using System.Collections.Generic;
using System.Linq; // Required for Count(r => ...)
using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    /// <summary>
    /// ViewModel for the admin dashboard that displays obstacle reports
    /// with filtering, sorting, and summary statistics.
    /// </summary>
    public class AdminDashboardViewModel
    {
        // --------------------------------------------------------------------
        // Data sources
        // --------------------------------------------------------------------

        /// <summary>
        /// The reports to render in the table (already filtered/sorted upstream).
        /// </summary>
        public List<ObstacleData> Reports { get; set; } = new();

        /// <summary>
        /// Administrators who can review and manage reports (optional helper list).
        /// </summary>
        public List<ApplicationUser> Administrators { get; set; } = new();

        /// <summary>
        /// Distinct organization names used to populate the organization filter.
        /// </summary>
        public List<string?> Organizations { get; set; } = new();

        // --------------------------------------------------------------------
        // Current UI state (kept so the view can preserve selected filters)
        // --------------------------------------------------------------------

        /// <summary>
        /// Current sort option. Example values: "date", "date-asc", "status", "name", etc.
        /// </summary>
        public string CurrentSort { get; set; } = "date";

        /// <summary>
        /// Current status filter. Default is "Pending" so the dashboard only shows
        /// items that still need attention unless the user changes the filter.
        /// Valid values: "all", "Pending", "Approved", "NotApproved".
        /// </summary>
        public string CurrentStatusFilter { get; set; } = "Pending";

        /// <summary>
        /// Current organization filter. "all" means no org filter applied.
        /// </summary>
        public string CurrentOrgFilter { get; set; } = "all";

        // --------------------------------------------------------------------
        // Summary statistics (computed from the current Reports collection)
        // --------------------------------------------------------------------

        /// <summary>
        /// Total reports in the current (filtered) collection.
        /// </summary>
        public int TotalReports => Reports.Count;

        /// <summary>
        /// Number of reports that are still waiting for review.
        /// </summary>
        public int PendingCount => Reports.Count(r => r.Status == ReportStatus.Pending);

        /// <summary>
        /// Number of reports that have been approved.
        /// </summary>
        public int ApprovedCount => Reports.Count(r => r.Status == ReportStatus.Approved);

        /// <summary>
        /// Number of reports that have been not approved (previously shown as "rejected").
        /// </summary>
        public int RejectedCount => Reports.Count(r => r.Status == ReportStatus.NotApproved);
    }
}
