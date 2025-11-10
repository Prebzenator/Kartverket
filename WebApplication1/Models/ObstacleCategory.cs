using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class ObstacleCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
