using System;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Plugins.Main.Test
{
    public class CompressorTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public CompressorTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Envelope()
        {
            var attackSamples = 1000;
            var y_T = new float[attackSamples];
            var attack = 1f;
            var alphaAttack = (float)Math.Exp(-1f / (attack * attackSamples));

            var input = -0.1f;

            y_T[0] = (1f - alphaAttack) * input;

            for (var i = 1; i < attackSamples; i++)
            {
                y_T[i] = alphaAttack * y_T[i - 1] + (1f - alphaAttack) * input;
            }

            for (var i = 0; i < attackSamples; i++)
            {
                _testOutputHelper.WriteLine(y_T[i].ToString());
            }
        }
    }
}