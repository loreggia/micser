using System.Text;

namespace Micser.Common.Extensions
{
    public static class StringBuilderExtensions
    {
        public static bool EndsWith(this StringBuilder sb, string str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                if (sb[sb.Length - i - 1] != str[str.Length - i - 1])
                {
                    return false;
                }
            }

            return true;
        }
    }
}