using Micser.Common.Extensions;
using Xunit;

namespace Micser.Common.Test.Extensions
{
    public class MathExtensionsTest
    {
        [Fact]
        public void InverseLerp()
        {
            var result = MathExtensions.InverseLerp(0f, 1f, 1f);
            Assert.Equal(1f, result);

            result = MathExtensions.InverseLerp(0f, 1f, 0f);
            Assert.Equal(0f, result);

            result = MathExtensions.InverseLerp(-1f, 1f, 0f);
            Assert.Equal(0.5f, result);

            result = MathExtensions.InverseLerp(1f, 10f, 1f);
            Assert.Equal(0f, result);
        }

        [Fact]
        public void Lerp()
        {
            var result = MathExtensions.Lerp(0f, 1f, 1f);
            Assert.Equal(1f, result);

            result = MathExtensions.Lerp(0f, 1f, 0f);
            Assert.Equal(0f, result);

            result = MathExtensions.Lerp(-1f, 1f, 0.5f);
            Assert.Equal(0f, result);

            result = MathExtensions.Lerp(2f, 4f, 0.5f);
            Assert.Equal(3f, result);
        }
    }
}