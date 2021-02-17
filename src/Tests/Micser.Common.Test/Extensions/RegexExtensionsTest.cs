using Micser.Common.Extensions;
using System.Text.RegularExpressions;
using Xunit;

namespace Micser.Common.Test.Extensions
{
    public class RegexExtensionsTest
    {
        [Fact]
        public void ReplaceGroup()
        {
            var regex = new Regex(@"Assembly(File)?Version\s*\(\s*""(?<version>[^""]+)""\s*\)");
            const string input = @"
                [assembly: AssemblyVersion(""1.0.0.0"")]
                [assembly: AssemblyFileVersion(""1.0.0.0"")]
            ";

            var output = regex.ReplaceGroup(input, "version", "1.2.3.4");

            Assert.Contains("1.2.3.4", output);
        }
    }
}