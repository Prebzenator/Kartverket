namespace WebApplication1.Helpers
{
        /// <summary>
        /// Helper class which provides methods to convert between meters and feet.
        /// Also includes optional formatting helpers for displaying the values as strings.
        /// Uses decimal for high precision, suitable for altitude and obstacle height conversions.
        /// </summary>
        
    public static class UnitConverter
    {
        private const decimal MeterPerFoot = 0.3048m;

        public static decimal? ToFeet(decimal? meters)
        {
            if (!meters.HasValue) return null;
            return meters.Value / MeterPerFoot;
        }
// Returns null if input is null
        public static decimal? ToMeters(decimal? feet)
        {
            if (!feet.HasValue) return null;
            return feet.Value * MeterPerFoot;
        }

// Feet not containing decimals
        public static string FormatFeet(decimal? feet, int decimals = 0)
            => feet.HasValue ? Math.Round(feet.Value, decimals).ToString($"F{decimals}") : "—";
// Meters with two decimals by default
        public static string FormatMeters(decimal? meters, int decimals = 2)
            => meters.HasValue ? Math.Round(meters.Value, decimals).ToString($"F{decimals}") : "—";
    }
}
