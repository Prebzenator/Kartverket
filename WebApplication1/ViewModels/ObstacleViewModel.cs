using WebApplication1.Helpers;

namespace WebApplication1.ViewModels
{
    public class ObstacleViewModel
    {
        // For display/edit: canonical stored value in meters (nullable)
        public decimal? HeightMeters { get; set; }

        // Input bound to the form. Use string to accept localized input (parse in controller).
        public string HeightInputRaw { get; set; } = string.Empty;

        // Optional other fields used in the form
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Coordinates { get; set; }

        // Calculated property: height in feet (nullable), uses decimal arithmetic from UnitConverter
        public decimal? HeightFeet => UnitConverter.ToFeet(HeightMeters);
    }
}
