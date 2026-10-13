using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class DuckerModuleTests
{
    [Test]
    public async Task WithoutSidechain_PassesInputUnchanged()
    {
        var ducker = new DuckerModule();
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-20f), null, ducker);

        var output = bench.Run(TimeSpan.FromSeconds(0.5));

        await Assert.That(PeakDb(output)).IsEqualTo(-20f).Within(0.01f);
    }

    [Test]
    public async Task SidechainAboveThreshold_LowersInputByDepth()
    {
        var ducker = new DuckerModule { Threshold = -40f, Depth = -15f };
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-20f), null, ducker);
        bench.AddSource(ducker.Sidechain, 300, Decibels.ToLinear(-20f));

        var output = bench.Run(TimeSpan.FromSeconds(0.5));

        await Assert.That(PeakDb(output)).IsEqualTo(-35f).Within(0.01f);
    }

    [Test]
    public async Task SidechainBelowThreshold_LeavesInputUnchanged()
    {
        var ducker = new DuckerModule { Threshold = -40f };
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-20f), null, ducker);
        bench.AddSource(ducker.Sidechain, 300, Decibels.ToLinear(-50f));

        var output = bench.Run(TimeSpan.FromSeconds(0.5));

        await Assert.That(PeakDb(output)).IsEqualTo(-20f).Within(0.01f);
    }

    [Test]
    public async Task Recovers_AfterHoldAndRelease()
    {
        var ducker = new DuckerModule
        {
            Threshold = -40f,
            Depth = -15f,
            Hold = 0.1f,
            Release = 0.01f,
        };
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-20f), null, ducker);
        var sidechain = bench.AddSource(ducker.Sidechain, 300, Decibels.ToLinear(-20f));
        bench.Run(TimeSpan.FromSeconds(0.3));

        sidechain.Amplitude = 0f;
        var output = bench.Run(TimeSpan.FromSeconds(0.5));

        // still down within the hold time, back once hold and release have passed
        var held = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(2400, 480)));
        await Assert.That(held).IsEqualTo(-35f).Within(0.01f);
        await Assert.That(PeakDb(output)).IsEqualTo(-20f).Within(0.01f);
    }

    [Test]
    public async Task Sidechain_IsNotMixedIntoTheOutput()
    {
        var ducker = new DuckerModule();
        var bench = new SignalTestBench(1000, 0f, null, ducker);
        bench.AddSource(ducker.Sidechain, 300, 1f);

        var output = bench.Run(TimeSpan.FromSeconds(0.1));

        await Assert.That(SignalTestBench.Peak(output)).IsEqualTo(0f);
    }

    private static float PeakDb(float[] output)
    {
        return Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)));
    }
}
