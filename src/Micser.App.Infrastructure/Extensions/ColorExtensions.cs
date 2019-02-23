using System;
using System.Drawing;

namespace Micser.App.Infrastructure.Extensions
{
    public static class ColorExtensions
    {
        public static Color FromAhsv(byte alpha, float hue, float saturation, float value)
        {
            if (hue < 0f || hue > 360f)
                throw new ArgumentOutOfRangeException(nameof(hue), hue, "Hue must be in the range [0,360]");
            if (saturation < 0f || saturation > 1f)
                throw new ArgumentOutOfRangeException(nameof(saturation), saturation, "Saturation must be in the range [0,1]");
            if (value < 0f || value > 1f)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be in the range [0,1]");

            int Component(int n)
            {
                var k = (n + hue / 60f) % 6;
                var c = value - value * saturation * Math.Max(Math.Min(Math.Min(k, 4 - k), 1), 0);
                var b = (int)Math.Round(c * 255);
                return b < 0 ? 0 : b > 255 ? 255 : b;
            }

            return Color.FromArgb(alpha, Component(5), Component(3), Component(1));
        }

        public static Color ToDrawing(this System.Windows.Media.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static System.Windows.Media.Color ToWpf(this Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}