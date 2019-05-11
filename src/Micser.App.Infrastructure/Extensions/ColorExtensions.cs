using System;
using System.Drawing;

namespace Micser.App.Infrastructure.Extensions
{
    /// <summary>
    /// Provides helper functions for color conversion.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Creates a <see cref="Color"/> from the specified alpha and HSV values.
        /// </summary>
        public static Color FromAhsv(byte alpha, float hue, float saturation, float value)
        {
            var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            var f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            var v = Convert.ToInt32(value);
            var p = Convert.ToInt32(value * (1 - saturation));
            var q = Convert.ToInt32(value * (1 - f * saturation));
            var t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
            {
                return Color.FromArgb(255, v, t, p);
            }

            if (hi == 1)
            {
                return Color.FromArgb(255, q, v, p);
            }

            if (hi == 2)
            {
                return Color.FromArgb(255, p, v, t);
            }

            if (hi == 3)
            {
                return Color.FromArgb(255, p, q, v);
            }

            if (hi == 4)
            {
                return Color.FromArgb(255, t, p, v);
            }

            return Color.FromArgb(255, v, p, q);
        }

        /// <summary>
        /// Gets the HSV value component of the color.
        /// </summary>
        public static float GetValue(this Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            return max / 255f;
        }

        /// <summary>
        /// Converts a <see cref="System.Windows.Media.Color"/> to a <see cref="Color"/>.
        /// </summary>
        public static Color ToDrawing(this System.Windows.Media.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// Converts a <see cref="Color"/> to a <see cref="System.Windows.Media.Color"/>.
        /// </summary>
        public static System.Windows.Media.Color ToWpf(this Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}