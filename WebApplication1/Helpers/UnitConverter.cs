namespace WebApplication1.Helpers
{
    public static class UnitConverter
    {
        private const decimal MeterPerFoot = 0.3048m;

        public static decimal? ToFeet(decimal? meters)
        {
            if (!meters.HasValue) return null;
            return meters.Value / MeterPerFoot;
        }

        public static decimal? ToMeters(decimal? feet)
        {
            if (!feet.HasValue) return null;
            return feet.Value * MeterPerFoot;
        }

        // Optional helpers for formatting output strings
        public static string FormatFeet(decimal? feet, int decimals = 0)
            => feet.HasValue ? Math.Round(feet.Value, decimals).ToString($"F{decimals}") : "—";

        public static string FormatMeters(decimal? meters, int decimals = 2)
            => meters.HasValue ? Math.Round(meters.Value, decimals).ToString($"F{decimals}") : "—";
    }
}
