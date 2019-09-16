namespace Micser.Common.Extensions
{
    /// <summary>
    /// Contains math related helper functions.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// Clamps a value to a range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <param name="max">The maximum value (inclusive).</param>
        public static void Clamp(ref float value, float min, float max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }
        }

        /// <summary>
        /// Clamps a value to a range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <param name="max">The maximum value (inclusive).</param>
        public static void Clamp(ref int value, int min, int max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }
        }

        /// <summary>
        /// Inverse linear interpolation (calculate the interpolation factor).
        /// </summary>
        /// <param name="min">The value where the result is 0.</param>
        /// <param name="max">The value where the result is 1.</param>
        /// <param name="value">The actual value.</param>
        /// <returns>The interpolation factor.</returns>
        public static float InverseLerp(float min, float max, float value)
        {
            return (value - min) / (max - min);
        }

        /// <summary>
        /// Linear interpolation between a minimum and a maximum.
        /// </summary>
        /// <param name="min">The minimum value (when <paramref name="amount"/> is 0).</param>
        /// <param name="max">The maximum value (when <paramref name="amount"/> is 1).</param>
        /// <param name="amount">The interpolation amount (0..1).</param>
        public static float Lerp(float min, float max, float amount)
        {
            return min * (1 - amount) + max * amount;
        }
    }
}