using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    /// <summary>
    /// ViewModel and EF entity for obstacle data registration.
    /// Used to both display the form and store submitted reports in the database.
    /// </summary>
    public class ObstacleData
    {
        /// <summary>
        /// Primary key for the database table.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the obstacle (e.g., "Windmill").
        /// </summary>
        [Required, StringLength(100)]
        [DisplayName("Obstacle name")]
        public string ObstacleName { get; set; } = string.Empty;

        /// <summary>
        /// Height of the obstacle in meters.
        /// Must be between 0 and 1000.
        /// </summary>
        [Required]
        [Range(0, 1000, ErrorMessage = "Height must be between 0 and 1000 meters.")]
        [DisplayName("Height (m)")]
        public decimal? ObstacleHeight { get; set; }

        /// <summary>
        /// Description of the obstacle. Max 500 characters.
        /// </summary>
        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500)]
        [DisplayName("Description")]
        [DataType(DataType.MultilineText)]
        public string? ObstacleDescription { get; set; }

        /// <summary>
        /// Form submission mode: true if saving as draft, false if final submission.
        /// </summary>
        [NotMapped]
        public bool IsDraft { get; set; }

        /// <summary>
        /// Latitude picked from map. Optional.
        /// </summary>
        [DisplayName("Latitude")]
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Longitude picked from map. Optional.
        /// </summary>
        [DisplayName("Longitude")]
        public decimal? Longitude { get; set; }

        /// <summary>
        /// UTC timestamp when the report was submitted by the user.
        /// </summary>
        [Required]
        [DisplayName("Reported at")]
        public DateTime ReportedAt { get; set; }

        /// <summary>
        /// UTC timestamp for when the obstacle was registered in the system.
        /// Automatically set at object creation.
        /// </summary>
        [Required]
        [DisplayName("Logged at")]
        public DateTime DateData { get; set; } = DateTime.UtcNow;

        // ===== USER TRACKING FIELDS =====

        /// <summary>
        /// Identity UserId of the reporter (foreign key to AspNetUsers).
        /// </summary>
        [StringLength(450)]
        [DisplayName("Reported by (User ID)")]
        public string? ReportedByUserId { get; set; }

        /// <summary>
        /// Full name of the reporter (cached for performance).
        /// </summary>
        [StringLength(100)]
        [DisplayName("Reporter name")]
        public string? ReporterName { get; set; }

        /// <summary>
        /// Organization of the reporter (cached for performance).
        /// </summary>
        [StringLength(100)]
        [DisplayName("Reporter organization")]
        public string? ReporterOrganization { get; set; }

        // ===== ADMIN WORKFLOW FIELDS =====

        /// <summary>
        /// Approval status of the report.
        /// </summary>
        [Required]
        [DisplayName("Status")]
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        /// <summary>
        /// UserId of the admin who is assigned to review this report.
        /// </summary>
        [StringLength(450)]
        [DisplayName("Assigned to (User ID)")]
        public string? AssignedToUserId { get; set; }

        /// <summary>
        /// Name of the admin assigned to this report (cached).
        /// </summary>
        [StringLength(100)]
        [DisplayName("Assigned to")]
        public string? AssignedToName { get; set; }

        /// <summary>
        /// Admin comments/feedback (required when rejecting).
        /// </summary>
        [StringLength(1000)]
        [DisplayName("Admin comments")]
        [DataType(DataType.MultilineText)]
        public string? AdminComments { get; set; }

        /// <summary>
        /// UserId of the admin who last reviewed/updated this report.
        /// </summary>
        [StringLength(450)]
        public string? ReviewedByUserId { get; set; }

        /// <summary>
        /// Name of the admin who last reviewed this report (cached).
        /// </summary>
        [StringLength(100)]
        [DisplayName("Reviewed by")]
        public string? ReviewedByName { get; set; }

        /// <summary>
        /// Timestamp of last status change by admin.
        /// </summary>
        [DisplayName("Last reviewed at")]
        public DateTime? LastReviewedAt { get; set; }
    }

    /// <summary>
    /// Enum defining the possible states of a submitted report.
    /// </summary>
    public enum ReportStatus
    {
        /// <summary>
        /// Report is awaiting review.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Report has been reviewed and approved.
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Report has been reviewed but not approved.
        /// </summary>
        NotApproved = 2
    }
}