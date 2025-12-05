using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    /// <summary>
    /// Entity representing an aviation obstacle report.
    /// </summary>
    public class ObstacleData
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [DisplayName("Obstacle name")]
        public string ObstacleName { get; set; } = string.Empty;

        [DisplayName("Category")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public ObstacleCategory? Category { get; set; }

        [Range(0, 1000, ErrorMessage = "Height must be between 0 and 1000 meters.")]
        [DisplayName("Height (m)")]
        public decimal? ObstacleHeight { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500)]
        [DisplayName("Description")]
        [DataType(DataType.MultilineText)]
        public string? ObstacleDescription { get; set; }

        [DisplayName("Latitude")]
        public decimal? Latitude { get; set; }

        [DisplayName("Longitude")]
        public decimal? Longitude { get; set; }

        [DisplayName("Geometry (GeoJSON)")]
        public string? GeometryJson { get; set; }

        [Required]
        [DisplayName("Reported at")]
        public DateTime ReportedAt { get; set; }

        [Required]
        [DisplayName("Logged at")]
        public DateTime DateData { get; set; } = DateTime.UtcNow;

        // ===== USER TRACKING FIELDS =====

        [StringLength(450)]
        [DisplayName("Reported by (User ID)")]
        public string? ReportedByUserId { get; set; }

        [StringLength(100)]
        [DisplayName("Reporter name")]
        public string? ReporterName { get; set; }

        [StringLength(100)]
        [DisplayName("Reporter organization")]
        public string? ReporterOrganization { get; set; }

        // ===== STATUS FIELD =====

        [Required]
        [DisplayName("Status")]
        public ReportStatus Status { get; set; } = ReportStatus.NotApproved;

        // ===== ADMIN WORKFLOW FIELDS =====

        [StringLength(450)]
        [DisplayName("Assigned to (User ID)")]
        public string? AssignedToUserId { get; set; }

        [StringLength(100)]
        [DisplayName("Assigned to")]
        public string? AssignedToName { get; set; }

        [StringLength(1000)]
        [DisplayName("Admin comments")]
        [DataType(DataType.MultilineText)]
        public string? AdminComments { get; set; }

        [StringLength(450)]
        public string? ReviewedByUserId { get; set; }

        [StringLength(100)]
        [DisplayName("Reviewed by")]
        public string? ReviewedByName { get; set; }

        [DisplayName("Last reviewed at")]
        public DateTime? LastReviewedAt { get; set; }

        /// <summary>
        /// Determines if a report is in a draft state (NotApproved and never reviewed by an admin).
        /// </summary>
        public bool IsProbablyDraft =>
            Status == ReportStatus.NotApproved &&
            string.IsNullOrEmpty(AdminComments) &&
            !LastReviewedAt.HasValue;
    }

    public enum ReportStatus
    {
        Pending = 0,
        Approved = 1,
        NotApproved = 2
    }
}