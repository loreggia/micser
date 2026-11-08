using Micser.Plugins.Main.Dsp;
using Micser.Plugins.Main.Modules;
using static Micser.Plugins.Main.Tests.Dsp.TestSignals;

namespace Micser.Plugins.Main.Tests.Dsp;

public class VoicePostEffectTests
{
    [Test]
    [Arguments(VoiceEffect.None)]
    [Arguments(VoiceEffect.Robot)]
    [Arguments(VoiceEffect.Whisper)]
    public async Task OtherEffects_LeaveTheSignal(VoiceEffect effect)
    {
        var input = Sine(440f);

        var output = Run(new VoicePostEffect(), effect, input);

        for (var i = 0; i < output.Length; i += 101)
        {
            await Assert.That(output[i]).IsEqualTo(input(i));
        }
    }

    [Test]
    public async Task Alien_AddsSidebandsAroundTheSignal()
    {
        var spectrum = Spectrum(Run(new VoicePostEffect(), VoiceEffect.Alien, Sine(1000f)));
        var signal = MagnitudeAt(spectrum, 1000f);

        await Assert
            .That(MagnitudeAt(spectrum, 1000f - (float)VoicePostEffect.AlienCarrier))
            .IsGreaterThan(0.5f * signal);
        await Assert
            .That(MagnitudeAt(spectrum, 1000f + (float)VoicePostEffect.AlienCarrier))
            .IsGreaterThan(0.5f * signal);
    }

    [Test]
    public async Task Radio_KeepsTheTelephoneBand()
    {
        var low = Run(new VoicePostEffect(), VoiceEffect.Radio, Sine(100f, 0.05f));
        var mid = Run(new VoicePostEffect(), VoiceEffect.Radio, Sine(1000f, 0.05f));

        await Assert.That(PeakOfSecondHalf(low)).IsLessThan(0.05f / 4f);
        await Assert.That(PeakOfSecondHalf(mid)).IsEqualTo(0.05f).Within(0.005f);
    }

    [Test]
    public async Task Radio_SoftClipsLoudSignals()
    {
        var output = Run(new VoicePostEffect(), VoiceEffect.Radio, Sine(1000f, 2f));

        await Assert.That(PeakOfSecondHalf(output)).IsLessThanOrEqualTo(0.5f);
        await Assert.That(PeakOfSecondHalf(output)).IsGreaterThan(0.45f);
    }

    private static float PeakOfSecondHalf(float[] samples)
    {
        return samples.Skip(samples.Length / 2).Max(Math.Abs);
    }

    private static float[] Run(VoicePostEffect postEffect, VoiceEffect effect, Func<int, float> input)
    {
        var output = new float[480 * 50];
        for (var i = 0; i < output.Length; i++)
        {
            output[i] = input(i);
        }

        for (var start = 0; start < output.Length; start += 480)
        {
            postEffect.Process(effect, (int)SampleRate, output.AsSpan(start, 480));
        }

        return output;
    }
}
