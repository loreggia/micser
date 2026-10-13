using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class NoiseGateModuleTests
{
    [Test]
    public async Task AboveThreshold_PassesSignalUnchanged()
    {
        var gate = new NoiseGateModule { Threshold = -40f };
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-20f), null, gate);

        var output = bench.Run(TimeSpan.FromSeconds(0.5));
        var peakDb = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)));

        await Assert.That(peakDb).IsEqualTo(-20f).Within(0.01f);
    }

    [Test]
    public async Task BelowThreshold_LowersSignalByRange()
    {
        var gate = new NoiseGateModule
        {
            Threshold = -40f,
            Range = -30f,
            Release = 0.05f,
        };
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-50f), null, gate);

        var output = bench.Run(TimeSpan.FromSeconds(1));
        var peakDb = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)));

        await Assert.That(peakDb).IsEqualTo(-80f).Within(0.1f);
    }

    [Test]
    public async Task Closes_AfterHoldAndRelease()
    {
        var gate = new NoiseGateModule
        {
            Threshold = -40f,
            Range = -60f,
            Hold = 0.1f,
            Release = 0.01f,
        };
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-20f), null, gate);
        bench.Run(TimeSpan.FromSeconds(0.2));

        bench.Amplitude = Decibels.ToLinear(-50f);
        var output = bench.Run(TimeSpan.FromSeconds(0.5));

        // still open within the hold time, closed once hold and release have passed
        var held = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(2400, 480)));
        var closed = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)));
        await Assert.That(held).IsEqualTo(-50f).Within(0.1f);
        await Assert.That(closed).IsEqualTo(-110f).Within(0.5f);
    }

    [Test]
    public async Task Hysteresis_KeepsGateOpenSlightlyBelowThreshold()
    {
        var gate = new NoiseGateModule
        {
            Threshold = -40f,
            Hysteresis = 6f,
            Hold = 0f,
        };
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-20f), null, gate);
        bench.Run(TimeSpan.FromSeconds(0.2));

        bench.Amplitude = Decibels.ToLinear(-43f);
        var output = bench.Run(TimeSpan.FromSeconds(0.5));
        var peakDb = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)));

        await Assert.That(peakDb).IsEqualTo(-43f).Within(0.1f);
    }

    [Test]
    public async Task Opens_WithinAttackTime()
    {
        var gate = new NoiseGateModule
        {
            Threshold = -40f,
            Range = -60f,
            Attack = 0.001f,
        };
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-60f), null, gate);
        bench.Run(TimeSpan.FromSeconds(0.5));

        bench.Amplitude = Decibels.ToLinear(-20f);
        var output = bench.Run(TimeSpan.FromSeconds(0.1));
        var peakDb = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(480, 480)));

        await Assert.That(peakDb).IsEqualTo(-20f).Within(0.1f);
    }

    [Test]
    public async Task LowFrequencies_DoNotChatterWithoutHold()
    {
        var gate = new NoiseGateModule
        {
            Threshold = -40f,
            Hold = 0f,
            Release = 0.001f,
        };
        var gated = new SignalTestBench(50, Decibels.ToLinear(-30f), null, gate).Run(TimeSpan.FromSeconds(0.5));
        var dry = new SignalTestBench(50, Decibels.ToLinear(-30f)).Run(TimeSpan.FromSeconds(0.5));

        // skips the start, where the gate opens
        var maxDifference = gated.Zip(dry, (a, b) => Math.Abs(a - b)).Skip(4800).Max();

        await Assert.That(maxDifference).IsLessThan(1e-6f);
    }
}
