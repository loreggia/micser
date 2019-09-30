using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Micser.Common.Extensions
{
    /// <summary>
    /// Contains extension methods for regular expressions.
    /// </summary>
    public static class RegexExtensions
    {
        /// <summary>
        /// Replaces only the value of a named group instead of the whole match by a replacement value.
        /// </summary>
        /// <param name="regex">The regular expression</param>
        /// <param name="input">The input string.</param>
        /// <param name="groupName">The group name whose value gets replaced.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <returns></returns>
        /// <remarks>
        /// https://stackoverflow.com/a/25513964/1511066
        /// </remarks>
        public static string ReplaceGroup(this Regex regex, string input, string groupName, string replacement)
        {
            return regex.Replace(
                input,
                m =>
                {
                    var group = m.Groups[groupName];
                    var sb = new StringBuilder();
                    var previousCaptureEnd = 0;

                    foreach (var capture in group.Captures.Cast<Capture>())
                    {
                        var currentCaptureEnd = capture.Index + capture.Length - m.Index;
                        var currentCaptureLength = capture.Index - m.Index - previousCaptureEnd;

                        sb.Append(m.Value.Substring(previousCaptureEnd, currentCaptureLength));
                        sb.Append(replacement);

                        previousCaptureEnd = currentCaptureEnd;
                    }

                    sb.Append(m.Value.Substring(previousCaptureEnd));

                    return sb.ToString();
                });
        }
    }
}