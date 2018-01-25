using System;
using System.Collections.Generic;

namespace Micser.Infrastructure.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
            {
                action(element);
            }
            return source;
        }
    }
}