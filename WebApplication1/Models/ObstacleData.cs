using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// ViewModel and EF entity for obstacle data registration
    /// Used to both display the form and store submitted reports in the database.
    public class ObstacleData
    {
        /// Primary key for the database table
        public int Id { get; set; }

        /// Use string for names and descriptions (e.g., "Windmill")
        [Required, StringLength(100)]
        [DisplayName("Obstacle name")]
        public string ObstacleName { get; set; } = string.Empty;

        /// Height of obstacle. Use decimal for inputs measured in meters.
        /// Decimal? and [Required] allows for validation. Error if null or out of range.
        /// Currently capped at 1000 meters for practicality. Can be adjusted as needed.
        [Required]
        [Range(0, 1000, ErrorMessage = "Height must be between 0 and 1000 meters.")]
        [DisplayName("Height (m)")]
        public decimal? ObstacleHeight { get; set; }

        /// Description of the obstacle. 500 characters max. Shown on overview page.
        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500)]
        [DisplayName("Description")]
        [DataType(DataType.MultilineText)]
        public string? ObstacleDescription { get; set; }

        /// Latitude picked from leaflet map. Decimal for precision.
        [DisplayName("Latitude")]
        public decimal? Latitude { get; set; }

        /// Longitude picked from leaflet map. Decimal for precision.
        [DisplayName("Longitude")]
        public decimal? Longitude { get; set; }

        /// UTC timestamp when the report was submitted by the user.
        /// This reflects when the obstacle was observed or occurred, as chosen by the user.
        [Required]
        [DisplayName("Reported at")]
        public DateTime ReportedAt { get; set; }

        /// UTC timestamp for when the obstacle was registered in the system.
        /// This is automatically set at object creation and used for logging and auditing.
        /// Required to ensure all obstacles have a system timestamp.
        [Required]
        [DisplayName("Logged at")]
        public DateTime DateData { get; set; } = DateTime.UtcNow;
    }
}
