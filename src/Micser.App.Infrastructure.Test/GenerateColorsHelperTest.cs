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
            var primary = Color.FromRgb(48, 152, 42);
            var secondary = Color.FromRgb(159, 195, 7);
            var neutral = Colors.White;

            _output.WriteLine(GetPaletteString(primary, "Primary", false, null, null));
            _output.WriteLine(GetPaletteString(secondary, "Secondary", false, null, null));
            _output.WriteLine(GetPaletteString(neutral, "Neutral", false, 0f));
        }

        private string GetPaletteString(Color color, string name, bool increasing = false, float? saturation = null, float? value = null)
        {
            var drawingColor = color.ToDrawing();
            var hue = drawingColor.GetHue();
            var sat = drawingColor.GetSaturation();
            var val = drawingColor.GetValue();
            const string template = "<Color x:Key=\"{{themes:ColorThemeKey {0}{1:000}}}\">{2}</Color>";
            var result = new StringBuilder();
            const int count = 10;

            for (var i = 0; i < count; i++)
            {
                var satStep = sat / count * i;
                var valStep = val / count * i;
                if (!increasing)
                {
                    satStep = sat - satStep;
                    valStep = val - valStep;
                }

                var iColor = ColorExtensions.FromAhsv(color.A, hue, saturation ?? satStep, value ?? valStep);
                result.AppendFormat(template, name, 100 - i * 10, iColor.ToWpf().ToString());
                result.AppendLine();
            }

            return result.ToString();
        }
    }
}