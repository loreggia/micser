using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class StereoToolsModuleTests
{
    [Test]
    public async Task Defaults_PassSignalUnchanged()
    {
        var (left, right) = Run(new StereoToolsModule(), 1f, 0.5f);

        await Assert.That(left).IsEqualTo(1f).Within(1e-5f);
        await Assert.That(right).IsEqualTo(0.5f).Within(1e-5f);
    }

    [Test]
    [Arguments(StereoMode.Swap, 0.5f, 1f)]
    [Arguments(StereoMode.LeftToBoth, 1f, 1f)]
    [Arguments(StereoMode.RightToBoth, 0.5f, 0.5f)]
    [Arguments(StereoMode.Mono, 0.75f, 0.75f)]
    public async Task Mode_RoutesTheChannels(StereoMode mode, float expectedLeft, float expectedRight)
    {
        var (left, right) = Run(new StereoToolsModule { Mode = mode }, 1f, 0.5f);

        await Assert.That(left).IsEqualTo(expectedLeft).Within(1e-5f);
        await Assert.That(right).IsEqualTo(expectedRight).Within(1e-5f);
    }

    [Test]
    public async Task InvertRight_AppliesBeforeTheMonoSum()
    {
        // right is the left inverted, so the plain sum cancels
        var (cancelled, _) = Run(new StereoToolsModule { Mode = StereoMode.Mono }, 1f, -1f);
        var (left, right) = Run(new StereoToolsModule { Mode = StereoMode.Mono, InvertRight = true }, 1f, -1f);

        await Assert.That(cancelled).IsEqualTo(0f).Within(1e-5f);
        await Assert.That(left).IsEqualTo(1f).Within(1e-5f);
        await Assert.That(right).IsEqualTo(1f).Within(1e-5f);
    }

    [Test]
    public async Task InvertLeft_InvertsOnlyTheLeftChannel()
    {
        var (left, right) = Run(new StereoToolsModule { InvertLeft = true }, 1f, 0.5f);

        await Assert.That(left).IsEqualTo(-1f).Within(1e-5f);
        await Assert.That(right).IsEqualTo(0.5f).Within(1e-5f);
    }

    [Test]
    [Arguments(0f, 0.75f, 0.75f)]
    [Arguments(2f, 1.25f, 0.25f)]
    public async Task Width_ScalesTheSideSignal(float width, float expectedLeft, float expectedRight)
    {
        // mid 0.75, side 0.25
        var (left, right) = Run(new StereoToolsModule { Width = width }, 1f, 0.5f);

        await Assert.That(left).IsEqualTo(expectedLeft).Within(1e-5f);
        await Assert.That(right).IsEqualTo(expectedRight).Within(1e-5f);
    }

    [Test]
    [Arguments(-0.5f, 1f, 0.25f)]
    [Arguments(1f, 0f, 0.5f)]
    public async Task Balance_LowersTheOtherSide(float balance, float expectedLeft, float expectedRight)
    {
        var (left, right) = Run(new StereoToolsModule { Balance = balance }, 1f, 0.5f);

        await Assert.That(left).IsEqualTo(expectedLeft).Within(1e-5f);
        await Assert.That(right).IsEqualTo(expectedRight).Within(1e-5f);
    }

    [Test]
    public async Task MonoInput_PassesUnchanged()
    {
        var bench = new SignalTestBench(440, 0.5f, null, new StereoToolsModule { Balance = 1f, InvertLeft = true });

        var output = bench.Run(TimeSpan.FromSeconds(0.1));

        await Assert.That(SignalTestBench.Peak(output)).IsEqualTo(0.5f).Within(1e-4f);
        await Assert.That(output.Skip(1).First()).IsGreaterThan(0f);
    }

    [Test]
    public async Task Changes_RampOverOneBlock()
    {
        var tools = new StereoToolsModule();
        var bench = CreateBench(tools, 1f, 0f);
        bench.Run(TimeSpan.FromSeconds(0.05));

        tools.Mode = StereoMode.Swap;
        var output = bench.Run(TimeSpan.FromSeconds(0.05));

        // left fades out over the first block instead of stopping at once; the 100 Hz sine moves less than 0.015 per sample
        var largestStep = output.Zip(output.Skip(1), (a, b) => Math.Abs(b - a)).Max();
        await Assert.That(largestStep).IsLessThan(0.02f);
        await Assert.That(SignalTestBench.Peak(output.AsSpan(480))).IsEqualTo(0f);
    }

    private static SignalTestBench CreateBench(StereoToolsModule tools, float leftGain, float rightGain)
    {
        return new SignalTestBench(100, 1f, ChannelLayout.Stereo, new ScaleChannels(leftGain, rightGain), tools);
    }

    /// <summary>
    /// Runs a sine with the given amplitudes on left and right through the module and returns the output peaks, signed by the polarity
    /// relative to the input sine.
    /// </summary>
    private static (float Left, float Right) Run(StereoToolsModule tools, float leftGain, float rightGain)
    {
        var left = CreateBench(tools, leftGain, rightGain);
        var leftOutput = left.Run(TimeSpan.FromSeconds(0.1));
        var rightTools = new StereoToolsModule();
        rightTools.SetState(tools.GetState());
        var right = CreateBench(rightTools, leftGain, rightGain);
        right.RecordedChannel = 1;
        var rightOutput = right.Run(TimeSpan.FromSeconds(0.1));
        var reference = new SignalTestBench(100, 1f).Run(TimeSpan.FromSeconds(0.1));

        return (SignedGain(leftOutput, reference), SignedGain(rightOutput, reference));
    }

    private static float SignedGain(float[] output, float[] reference)
    {
        // the last 960 frames are two periods of the 100 Hz sine, well after the first block's ramp
        var start = output.Length - 960;
        var dot = 0f;
        var energy = 0f;
        for (var i = start; i < output.Length; i++)
        {
            dot += output[i] * reference[i];
            energy += reference[i] * reference[i];
        }

        return dot / energy;
    }

    private sealed class ScaleChannels : EffectModule
    {
        private readonly float _left;
        private readonly float _right;

        public ScaleChannels(float left, float right)
        {
            _left = left;
            _right = right;
        }

        protected override void Process(AudioBuffer buffer)
        {
            Scale(buffer.GetChannel(0), _left);
            Scale(buffer.GetChannel(1), _right);
        }

        private static void Scale(Span<float> samples, float factor)
        {
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] *= factor;
            }
        }
    }
}
