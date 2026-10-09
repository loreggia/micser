using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class CompressorModuleTests
{
    [Test]
    public async Task Amount_Zero_LeavesSignalUnchanged()
    {
        var compressor = new CompressorModule
        {
            Threshold = -20f,
            Ratio = 4f,
            Amount = 0f,
        };
        var bench = new SignalTestBench(1000, 1f, null, compressor);

        var output = bench.Run(TimeSpan.FromSeconds(0.5));

        await Assert.That(SignalTestBench.Peak(output)).IsEqualTo(1f).Within(1e-4f);
    }

    [Test]
    public async Task Downward_LeavesLevelBelowThresholdUnchanged()
    {
        var compressor = new CompressorModule
        {
            Threshold = -20f,
            Ratio = 4f,
            Knee = 0f,
        };
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-40f), null, compressor);

        var output = bench.Run(TimeSpan.FromSeconds(0.5));
        var peakDb = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)));

        await Assert.That(peakDb).IsEqualTo(-40f).Within(0.1f);
    }

    [Test]
    public async Task Downward_ReducesLevelAboveThresholdByRatio()
    {
        var compressor = new CompressorModule
        {
            Threshold = -20f,
            Ratio = 4f,
            Knee = 0f,
            Attack = 0.001f,
            Release = 0.05f,
        };
        var bench = new SignalTestBench(1000, 1f, null, compressor);

        var output = bench.Run(TimeSpan.FromSeconds(1));
        var peakDb = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)));

        // 0 dB in, 20 dB above the threshold: -20 + 20 / 4 = -15 dB (the peak detector smooths, so allow some slack)
        await Assert.That(peakDb).IsBetween(-16.5f, -13.5f);
    }

    [Test]
    public async Task InvertedStereoChannels_AreStillDetected()
    {
        var compressor = new CompressorModule
        {
            Threshold = -20f,
            Ratio = 4f,
            Knee = 0f,
        };
        var inverter = new InvertSecondChannel();
        var bench = new SignalTestBench(1000, 1f, ChannelLayout.Stereo, inverter, compressor);

        var output = bench.Run(TimeSpan.FromSeconds(1));

        await Assert.That(SignalTestBench.Peak(output.AsSpan(output.Length - 4800))).IsLessThan(0.5f);
    }

    [Test]
    public async Task Upward_RaisesLevelBelowThreshold()
    {
        var compressor = new CompressorModule
        {
            Type = CompressorType.Upward,
            Threshold = -20f,
            Ratio = 2f,
            Knee = 0f,
        };
        var bench = new SignalTestBench(1000, Decibels.ToLinear(-40f), null, compressor);

        var output = bench.Run(TimeSpan.FromSeconds(1));
        var peakDb = Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)));

        await Assert.That(peakDb).IsGreaterThan(-36f);
    }

    private sealed class InvertSecondChannel : EffectModule
    {
        protected override void Process(AudioBuffer buffer)
        {
            var channel = buffer.GetChannel(1);
            for (var i = 0; i < channel.Length; i++)
            {
                channel[i] = -channel[i];
            }
        }
    }
}
