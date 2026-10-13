using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class LimiterModuleTests
{
    // the delay of the output: one frame less than the lookahead window
    private static readonly int Latency =
        (int)MathF.Round(LimiterModule.Lookahead * SignalTestBench.Format.SampleRate) - 1;

    [Test]
    public async Task BelowCeiling_PassesSignalUnchangedButDelayed()
    {
        var output = new SignalTestBench(1000, 0.5f, null, new LimiterModule()).Run(TimeSpan.FromSeconds(0.1));
        var dry = new SignalTestBench(1000, 0.5f).Run(TimeSpan.FromSeconds(0.1));

        await Assert.That(SignalTestBench.Peak(output.AsSpan(0, Latency))).IsEqualTo(0f);
        await Assert.That(MaxDifference(output.AsSpan(Latency), dry, 1f)).IsLessThan(1e-6f);
    }

    [Test]
    [Arguments(-1f, 0f)]
    [Arguments(-6f, 12f)]
    public async Task LoudSignal_StaysAtTheCeilingWithoutDistortion(float ceiling, float gain)
    {
        var limiter = new LimiterModule { Ceiling = ceiling, Gain = gain };
        var output = new SignalTestBench(1000, 2f, null, limiter).Run(TimeSpan.FromSeconds(0.5));
        var dry = new SignalTestBench(1000, 2f).Run(TimeSpan.FromSeconds(0.5));

        // once the gain has settled, the output is the delayed sine scaled down, not clipped
        var start = output.Length - 4800;
        var peak = SignalTestBench.Peak(output.AsSpan(start));
        var difference = MaxDifference(output.AsSpan(start), dry.AsSpan(start - Latency), peak / 2f);
        await Assert.That(Decibels.FromLinear(peak)).IsEqualTo(ceiling).Within(0.01f);
        await Assert.That(difference).IsLessThan(1e-4f);
    }

    [Test]
    public async Task SuddenPeak_IsCaughtByTheLookahead()
    {
        var limiter = new LimiterModule { Ceiling = -1f };
        var bench = new SignalTestBench(1000, 0.1f, null, limiter);
        bench.Run(TimeSpan.FromSeconds(0.2));

        bench.Amplitude = 8f;
        var output = bench.Run(TimeSpan.FromSeconds(0.1));

        var ceiling = Decibels.ToLinear(-1f);
        await Assert.That(SignalTestBench.Peak(output)).IsLessThanOrEqualTo(ceiling);
        // a clipped sine would have flat tops: consecutive samples at the ceiling
        var flatTops = output
            .Zip(output.Skip(1))
            .Count(p => Math.Abs(p.First) > ceiling - 1e-4f && Math.Abs(p.Second) > ceiling - 1e-4f);
        await Assert.That(flatTops).IsEqualTo(0);
    }

    [Test]
    public async Task Gain_RecoversAfterRelease()
    {
        var limiter = new LimiterModule { Ceiling = -1f, Release = 0.05f };
        var bench = new SignalTestBench(1000, 4f, null, limiter);
        bench.Run(TimeSpan.FromSeconds(0.2));

        bench.Amplitude = 0.1f;
        var output = bench.Run(TimeSpan.FromSeconds(0.5));

        await Assert.That(SignalTestBench.Peak(output.AsSpan(output.Length - 4800))).IsEqualTo(0.1f).Within(1e-4f);
    }

    [Test]
    public async Task Gain_RaisesTheInput()
    {
        var limiter = new LimiterModule { Gain = 6f };
        var output = new SignalTestBench(1000, 0.1f, null, limiter).Run(TimeSpan.FromSeconds(0.2));

        var peakDb = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)));
        await Assert.That(peakDb).IsEqualTo(-20f + 6f).Within(0.01f);
    }

    private static float MaxDifference(ReadOnlySpan<float> output, ReadOnlySpan<float> reference, float scale)
    {
        var max = 0f;
        for (var i = 0; i < Math.Min(output.Length, reference.Length); i++)
        {
            max = Math.Max(max, Math.Abs(output[i] - (reference[i] * scale)));
        }

        return max;
    }
}
