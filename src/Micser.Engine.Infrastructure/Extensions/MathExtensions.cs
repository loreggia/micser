namespace Micser.Engine.Infrastructure.Extensions
{
    public static class MathExtensions
    {
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
    }
}