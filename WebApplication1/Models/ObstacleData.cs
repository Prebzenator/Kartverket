using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models

/// ViewModel for obstacle data registration
/// Used to both display the form and show the overview of submitted data
/// 
{
    public class ObstacleData
    {
        // Use string for names and descriptions (e.g., "Windmill")
        [Required, StringLength(100)]
        [DisplayName("Obstacle name")]
        public string ObstacleName { get; set; } = string.Empty;

        /// Height of obstacle. Use decimal for inputs measured in meters. Decimal? and [Required] allows for validation. Error if null or out of range.
        /// Currently capped at 1000 meters for practicality. Can be adjusted as needed.
        [Required]
        [Range(0, 1000, ErrorMessage = "Height must be between 0 and 1000 meters.")]
        [DisplayName("Height (m)")]
        public decimal? ObstacleHeight { get; set; }

        // Description of the obstacle. 500 characters max. Shown on overview page.
        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500)]
        [DisplayName("Description")]
        [DataType(DataType.MultilineText)]

        public string? ObstacleDescription { get; set; }
        // Latitude picked from leaflet map. Decimal for precision
        [DisplayName("Latitude")]
        public decimal? Latitude { get; set; }
        // Longitude picked from leaflet map. Decimal for precision
        [DisplayName("Longitude")]
        public decimal? Longitude { get; set; }

    }
}
