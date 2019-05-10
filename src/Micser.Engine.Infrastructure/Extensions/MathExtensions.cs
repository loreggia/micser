namespace Micser.Engine.Infrastructure.Extensions
{
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