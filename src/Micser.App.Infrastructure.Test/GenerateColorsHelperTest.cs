using Micser.App.Infrastructure.Extensions;
using System.Text;
using System.Windows.Media;
using Xunit;
using Xunit.Abstractions;

namespace Micser.App.Infrastructure.Test
{
    public class GenerateColorsHelperTest
    {
        private readonly ITestOutputHelper _output;

        public GenerateColorsHelperTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GenerateColorsTest()
        {
            var primary = Color.FromRgb(254, 135, 0);
            var secondary = Color.FromRgb(0, 149, 255);
            var neutral = Colors.Black;

            _output.WriteLine(GetPaletteString(primary, "Primary", false, null, 1f));
            _output.WriteLine(GetPaletteString(secondary, "Secondary", false, null, 1f));
            _output.WriteLine(GetPaletteString(neutral, "Neutral", true, 0f));
        }

        private string GetPaletteString(Color color, string name, bool increasing = false, float? saturation = null, float? value = null)
        {
            var drawingColor = color.ToDrawing();
            var hue = drawingColor.GetHue();
            const string template = "<Color x:Key=\"{{themes:ColorThemeKey {0}{1:000}}}\">{2}</Color>";
            var result = new StringBuilder();
            const int count = 10;

            for (var i = 0; i < count; i++)
            {
                var step = 1f / count * i;
                if (!increasing)
                {
                    step = 1f - step;
                }

                var iColor = ColorExtensions.FromAhsv(color.A, hue, saturation ?? step, value ?? step);
                result.AppendFormat(template, name, 100 - i * 10, iColor.ToWpf().ToString());
                result.AppendLine();
            }

            return result.ToString();
        }
    }
}