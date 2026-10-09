using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class GainModuleTests
{
    [Test]
    public async Task Gain_KeepsChannelLayout()
    {
        var bench = new SignalTestBench(1000, 0.25f, ChannelLayout.Surround51, new GainModule { Gain = -6f });

        bench.Run(TimeSpan.FromMilliseconds(10));

        await Assert.That(bench.OutputLayout).IsEqualTo(ChannelLayout.Surround51);
    }

    [Test]
    public async Task Gain_ScalesByDecibels()
    {
        var bench = new SignalTestBench(1000, 0.25f, null, new GainModule { Gain = 6f });

        var output = bench.Run(TimeSpan.FromMilliseconds(100));

        await Assert
            .That(SignalTestBench.Peak(output.AsSpan(SignalTestBench.Format.FrameCount)))
            .IsEqualTo(0.25f * Decibels.ToLinear(6f))
            .Within(0.002f);
    }
}
