using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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

        /// <summary>
        /// Approval status of the report.
        /// </summary>
        [Required]
        [DisplayName("Status")]
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
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
