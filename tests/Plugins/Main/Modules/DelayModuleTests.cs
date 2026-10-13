using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class DelayModuleTests
{
    [Test]
    public async Task ZeroDelay_PassesSignalUnchanged()
    {
        var output = new SignalTestBench(1000, 0.5f, null, new DelayModule()).Run(TimeSpan.FromSeconds(0.1));
        var dry = new SignalTestBench(1000, 0.5f).Run(TimeSpan.FromSeconds(0.1));

        await Assert.That(output).IsEquivalentTo(dry);
    }

    [Test]
    [Arguments(0.01f)]
    [Arguments(0.0123f)]
    [Arguments(1.5f)]
    public async Task Time_DelaysAllChannels(float time)
    {
        var frames = (int)MathF.Round(time * SignalTestBench.Format.SampleRate);
        var delay = new DelayModule { Time = time };
        var bench = new SignalTestBench(440, 0.5f, ChannelLayout.Stereo, delay) { RecordedChannel = 1 };

        var output = bench.Run(TimeSpan.FromSeconds(time + 0.1));
        var dry = new SignalTestBench(440, 0.5f).Run(TimeSpan.FromSeconds(time + 0.1));

        await Assert.That(SignalTestBench.Peak(output.AsSpan(0, frames))).IsEqualTo(0f);
        await Assert
            .That(MaxDifference(output.AsSpan(frames), dry.AsSpan(0, output.Length - frames)))
            .IsLessThan(1e-6f);
    }

    [Test]
    public async Task Channels_AddToTheDelayOfTheirChannel()
    {
        var delay = new DelayModule { Time = 0.01f, Channels = [new DelayChannel(0f), new DelayChannel(0.005f)] };
        var left = new SignalTestBench(440, 0.5f, ChannelLayout.Stereo, delay);
        var leftOutput = left.Run(TimeSpan.FromSeconds(0.1));
        var right = new SignalTestBench(
            440,
            0.5f,
            ChannelLayout.Stereo,
            new DelayModule { Time = 0.01f, Channels = delay.Channels }
        )
        {
            RecordedChannel = 1,
        };
        var rightOutput = right.Run(TimeSpan.FromSeconds(0.1));
        var dry = new SignalTestBench(440, 0.5f).Run(TimeSpan.FromSeconds(0.1));

        await Assert.That(MaxDifference(leftOutput.AsSpan(480), dry.AsSpan(0, dry.Length - 480))).IsLessThan(1e-6f);
        await Assert.That(MaxDifference(rightOutput.AsSpan(720), dry.AsSpan(0, dry.Length - 720))).IsLessThan(1e-6f);
    }

    [Test]
    public async Task TotalDelay_IsLimitedToMaxTime()
    {
        var delay = new DelayModule { Time = DelayModule.MaxTime, Channels = [new DelayChannel(0.1f)] };
        var bench = new SignalTestBench(440, 0.5f, null, delay);
        var frames = (int)(DelayModule.MaxTime * SignalTestBench.Format.SampleRate);

        var output = bench.Run(TimeSpan.FromSeconds(DelayModule.MaxTime + 0.1));
        var dry = new SignalTestBench(440, 0.5f).Run(TimeSpan.FromSeconds(DelayModule.MaxTime + 0.1));

        await Assert
            .That(MaxDifference(output.AsSpan(frames), dry.AsSpan(0, output.Length - frames)))
            .IsLessThan(1e-6f);
    }

    [Test]
    public async Task ChangingTheTime_Crossfades()
    {
        var delay = new DelayModule { Time = 0.01f };
        var bench = new SignalTestBench(100, 1f, null, delay);
        bench.Run(TimeSpan.FromSeconds(0.1));

        delay.Time = 0.0153f;
        var output = bench.Run(TimeSpan.FromSeconds(0.1));

        // a jump between the two delayed signals would be far larger than the steps of the 100 Hz sine (about 0.013)
        var largestStep = output.Zip(output.Skip(1), (a, b) => Math.Abs(b - a)).Max();
        await Assert.That(largestStep).IsLessThan(0.03f);
    }

    [Test]
    public async Task SetState_CopiesTheChannels()
    {
        var delay = new DelayModule();
        var state = new DelayState(0.2f, [new DelayChannel(0.01f), new DelayChannel(0f)]);

        delay.SetState(state);

        await Assert.That(delay.GetState().Time).IsEqualTo(0.2f);
        await Assert.That(delay.GetState().Channels).IsEquivalentTo(state.Channels);
        await Assert.That(delay.Channels).IsNotSameReferenceAs(state.Channels);
    }

    private static float MaxDifference(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var max = 0f;
        for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            max = Math.Max(max, Math.Abs(a[i] - b[i]));
        }

        return max;
    }
}
