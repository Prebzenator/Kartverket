using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class ObstacleData
    {
        [Required, StringLength(100)]
        [DisplayName("Obstacle name")]
        public string ObstacleName { get; set; } = string.Empty;

        // Use decimal for inputs measured in meters; better for user-entered numeric values than double.
        [Required]
        [Range(0, 1000, ErrorMessage = "Height must be between 0 and 1000 meters.")]
        [DisplayName("Height (m)")]
        public decimal? ObstacleHeight { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500)]
        [DisplayName("Description")]
        [DataType(DataType.MultilineText)]
        public string? ObstacleDescription { get; set; }
    }
}
