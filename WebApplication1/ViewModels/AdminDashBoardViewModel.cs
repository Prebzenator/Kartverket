using WebApplication1.Models;

namespace WebApplication1.ViewModels
{
    /// <summary>
    /// ViewModel for the admin dashboard showing all reports with filtering/sorting options.
    /// </summary>
    public class AdminDashboardViewModel
    {
        public List<ObstacleData> Reports { get; set; } = new();
        public List<ApplicationUser> Administrators { get; set; } = new();
        public List<string?> Organizations { get; set; } = new();

        public string CurrentSort { get; set; } = "date";
        public string CurrentStatusFilter { get; set; } = "all";
        public string CurrentOrgFilter { get; set; } = "all";
        public string CurrentCategoryFilter { get; set; } = "all";
        public string? Query { get; set; }

        // Statistics
        public int TotalReports => Reports.Count;
        public int PendingCount => Reports.Count(r => r.Status == ReportStatus.Pending);
        public int ApprovedCount => Reports.Count(r => r.Status == ReportStatus.Approved);
        public int RejectedCount => Reports.Count(r => r.Status == ReportStatus.NotApproved);
    }
}