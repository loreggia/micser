using Micser.Plugins.Main.Dsp;

namespace Micser.Plugins.Main.Tests.Dsp;

public class FftTests
{
    [Test]
    [Arguments(8)]
    [Arguments(256)]
    public async Task Forward_Cosine_HasPeaksAtItsBin(int size)
    {
        var fft = new Fft(1024);
        var data = new float[2 * size];
        for (var n = 0; n < size; n++)
        {
            data[2 * n] = MathF.Cos(2 * MathF.PI * 3 * n / size);
        }

        fft.Transform(data, size, inverse: false);

        for (var k = 0; k < size; k++)
        {
            var magnitude = MathF.Sqrt((data[2 * k] * data[2 * k]) + (data[(2 * k) + 1] * data[(2 * k) + 1]));
            var expected = k == 3 || k == size - 3 ? size / 2f : 0f;
            await Assert.That(magnitude).IsEqualTo(expected).Within(1e-3f * size);
        }
    }

    [Test]
    public async Task Inverse_AfterForward_ScalesBySize()
    {
        var fft = new Fft(64);
        var random = new Random(1);
        var original = new float[2 * 64];
        for (var i = 0; i < original.Length; i++)
        {
            original[i] = (float)random.NextDouble() - 0.5f;
        }

        var data = (float[])original.Clone();
        fft.Transform(data, 64, inverse: false);
        fft.Transform(data, 64, inverse: true);

        for (var i = 0; i < data.Length; i++)
        {
            await Assert.That(data[i] / 64).IsEqualTo(original[i]).Within(1e-5f);
        }
    }

    [Test]
    public async Task Transform_SizeNotAPowerOfTwo_Throws()
    {
        var fft = new Fft(64);

        await Assert
            .That(() => fft.Transform(new float[2 * 48], 48, inverse: false))
            .Throws<ArgumentOutOfRangeException>();
    }
}
