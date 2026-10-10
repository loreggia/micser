using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class FilterModuleTests
{
    [Test]
    [Arguments(FilterType.LowPass, 12)]
    [Arguments(FilterType.LowPass, 48)]
    [Arguments(FilterType.HighPass, 12)]
    [Arguments(FilterType.HighPass, 48)]
    public async Task Cutoff_Is3DbDown(FilterType type, int slope)
    {
        var filter = new FilterModule
        {
            Type = type,
            Frequency = 1000,
            Slope = slope,
        };
        var bench = new SignalTestBench(1000, 0.1f, null, filter);

        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert.That(LevelOfLast100Ms(output, 0.1f)).IsEqualTo(-3.01f).Within(0.1f);
    }

    [Test]
    [Arguments(FilterType.LowPass, 12, 0.5f)]
    [Arguments(FilterType.LowPass, 24, 2f)]
    [Arguments(FilterType.LowPass, 48, 4f)]
    [Arguments(FilterType.HighPass, 12, 4f)]
    [Arguments(FilterType.HighPass, 36, 0.5f)]
    public async Task GainAtCutoff_IsQ(FilterType type, int slope, float q)
    {
        var filter = new FilterModule
        {
            Type = type,
            Frequency = 1000,
            Slope = slope,
            Q = q,
        };
        var bench = new SignalTestBench(1000, 0.1f, null, filter);

        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert.That(LevelOfLast100Ms(output, 0.1f)).IsEqualTo(Decibels.FromLinear(q)).Within(0.1f);
    }

    [Test]
    public async Task ChangingFrequency_TakesEffect()
    {
        var filter = new FilterModule { Type = FilterType.LowPass, Frequency = 200 };
        var bench = new SignalTestBench(2000, 0.1f, null, filter);
        bench.Run(TimeSpan.FromMilliseconds(200));

        filter.Frequency = 20000;
        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert.That(LevelOfLast100Ms(output, 0.1f)).IsEqualTo(0f).Within(0.1f);
    }

    [Test]
    public async Task ChangingQ_TakesEffect()
    {
        var filter = new FilterModule { Type = FilterType.LowPass, Frequency = 1000 };
        var bench = new SignalTestBench(1000, 0.1f, null, filter);
        bench.Run(TimeSpan.FromMilliseconds(200));

        filter.Q = 4f;
        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert.That(LevelOfLast100Ms(output, 0.1f)).IsEqualTo(Decibels.FromLinear(4f)).Within(0.1f);
    }

    [Test]
    public async Task ChangingType_TakesEffect()
    {
        var filter = new FilterModule { Type = FilterType.LowPass, Frequency = 1000 };
        var bench = new SignalTestBench(100, 0.1f, null, filter);
        bench.Run(TimeSpan.FromMilliseconds(200));

        filter.Type = FilterType.HighPass;
        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert.That(LevelOfLast100Ms(output, 0.1f)).IsLessThan(-35f);
    }

    [Test]
    public async Task FrequencyAboveNyquist_IsLimited()
    {
        var filter = new FilterModule { Type = FilterType.LowPass, Frequency = 30000 };
        var bench = new SignalTestBench(1000, 0.1f, null, filter);

        var output = bench.Run(TimeSpan.FromMilliseconds(200));

        await Assert.That(LevelOfLast100Ms(output, 0.1f)).IsEqualTo(0f).Within(0.1f);
    }

    [Test]
    public async Task HighPass_PassesHighFrequencies()
    {
        var bench = new SignalTestBench(5000, 0.1f, null, new FilterModule { Frequency = 80 });

        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert.That(LevelOfLast100Ms(output, 0.1f)).IsEqualTo(0f).Within(0.1f);
    }

    [Test]
    public async Task LowPass_PassesLowFrequencies()
    {
        var filter = new FilterModule { Type = FilterType.LowPass, Frequency = 5000 };
        var bench = new SignalTestBench(100, 0.1f, null, filter);

        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert.That(LevelOfLast100Ms(output, 0.1f)).IsEqualTo(0f).Within(0.1f);
    }

    [Test]
    [Arguments(12)]
    [Arguments(24)]
    [Arguments(36)]
    [Arguments(48)]
    public async Task OctaveAboveLowPassCutoff_IsAttenuatedBySlope(int slope)
    {
        var filter = new FilterModule
        {
            Type = FilterType.LowPass,
            Frequency = 1000,
            Slope = slope,
        };
        var bench = new SignalTestBench(2000, 0.1f, null, filter);

        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        // Butterworth response of order slope / 6 at the bilinear transform's warped frequency ratio
        var ratio = Math.Tan(Math.PI * 2000 / 48000) / Math.Tan(Math.PI * 1000 / 48000);
        var expected = (float)(-10 * Math.Log10(1 + Math.Pow(ratio, 2 * slope / 6.0)));
        await Assert.That(LevelOfLast100Ms(output, 0.1f)).IsEqualTo(expected).Within(0.2f);
    }

    private static float LevelOfLast100Ms(float[] output, float amplitude)
    {
        return Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)) / amplitude);
    }
}
