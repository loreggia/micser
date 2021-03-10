namespace Micser.Common.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="string"/> instances.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a string to its camel case representation (the first character is converted to lower case).
        /// </summary>
        /// <param name="str">The source string.</param>
        public static string ToCamelCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && str.Length > 1)
            {
                return char.ToLowerInvariant(str[0]) + str[1..];
            }

            return str;
        }
    }
}