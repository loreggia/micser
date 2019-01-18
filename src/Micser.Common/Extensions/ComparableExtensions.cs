using System;

namespace Micser.Common.Extensions
{
    public static class ComparableExtensions
    {
        public static T Clamp<T>(this T value, T min, T max)
            where T : IComparable<T>
        {
            return value.CompareTo(min) < 0 ? min : value.ClampMax(max);
        }

        public static T ClampMax<T>(this T value, T max)
            where T : IComparable<T>
        {
            return value.CompareTo(max) > 0 ? max : value;
        }

        public static T ClampMin<T>(this T value, T min)
            where T : IComparable<T>
        {
            return value.CompareTo(min) < 0 ? min : value;
        }
    }
}