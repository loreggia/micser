using System;
using System.Linq.Expressions;

namespace Micser.Common.Extensions
{
    public static class PropertyExtensions
    {
        public static string GetName<T>(Expression<Func<T>> property)
        {
            if (!(property.Body is MemberExpression me))
            {
                throw new ArgumentException(nameof(property));
            }
            return me.Member.Name;
        }
    }
}