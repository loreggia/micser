using Micser.Plugins.Main.Dsp;
using static Micser.Plugins.Main.Tests.Dsp.TestSignals;

namespace Micser.Plugins.Main.Tests.Dsp;

public class VoiceShifterTests
{
    private const int BlockSize = 480;

    [Test]
    public async Task FormantsWithPitch_OneOctaveUp_DoublesFrequency()
    {
        var output = Run(new VoiceShifter(2048), Sine(440f), new VoiceParameters(2f, 2f, 1f));

        await Assert.That(PeakFrequency(Spectrum(output))).IsEqualTo(880f).Within(10f);
    }

    [Test]
    public async Task Formant_Up_MovesTheFormantAndKeepsThePitch()
    {
        var factor = MathF.Pow(2f, 5f / 12f);

        var output = Run(new VoiceShifter(2048), Vowel(150f, 1000f), new VoiceParameters(1f, factor, 1f));

        // the strongest harmonic is still a multiple of 150 Hz, but near the moved formant
        var peak = PeakFrequency(Spectrum(output));
        await Assert.That(peak).IsEqualTo(1000f * factor).Within(150f);
        await Assert.That(DistanceToHarmonic(peak, 150f)).IsLessThan(10f);
    }

    [Test]
    public async Task Pitch_Up_KeepsTheFormant()
    {
        var factor = MathF.Pow(2f, 5f / 12f);

        var output = Run(new VoiceShifter(2048), Vowel(150f, 1000f), new VoiceParameters(factor, 1f, 1f));

        // the harmonics are multiples of the new pitch, the strongest still near the formant
        var peak = PeakFrequency(Spectrum(output));
        await Assert.That(peak).IsEqualTo(1000f).Within(150f);
        await Assert.That(DistanceToHarmonic(peak, 150f * factor)).IsLessThan(10f);
    }

    [Test]
    public async Task Mix_Zero_DelaysTheDrySignalByTheLatency()
    {
        var input = Sine(440f);
        var output = Run(new VoiceShifter(2048), input, new VoiceParameters(2f, 2f, 0f));
        var latency = VoiceShifter.GetLatency(1024, 4);

        await Assert.That(output.Take(latency)).All(s => s == 0f);
        for (var i = latency; i < output.Length; i += 97)
        {
            await Assert.That(output[i]).IsEqualTo(input(i - latency));
        }
    }

    [Test]
    public async Task Reset_ProcessesLikeANewInstance()
    {
        var parameters = new VoiceParameters(1.2f, 1.1f, 1f, 0.5f, 0.5f);
        var used = new VoiceShifter(2048);
        Run(used, Sine(300f), new VoiceParameters(1.5f, 0.8f, 1f, 1f, 1f, VoiceTexture.Robot));
        used.Reset();

        var afterReset = Run(used, Vowel(150f, 1000f), parameters);
        var fresh = Run(new VoiceShifter(2048), Vowel(150f, 1000f), parameters);

        await Assert.That(afterReset).IsEquivalentTo(fresh);
    }

    [Test]
    public async Task OversamplingChange_ProcessesLikeANewInstance()
    {
        var parameters = new VoiceParameters(1.2f, 1.1f, 1f);
        var used = new VoiceShifter(2048);
        Run(used, Sine(300f), new VoiceParameters(1.5f, 0.8f, 1f));

        var changed = Run(used, Vowel(150f, 1000f), parameters, 8);
        var fresh = Run(new VoiceShifter(2048), Vowel(150f, 1000f), parameters, 8);

        await Assert.That(changed).IsEquivalentTo(fresh);
    }

    [Test]
    public async Task Wobble_VariesThePitch()
    {
        var steady = FrequencyRange(Run(new VoiceShifter(2048), Sine(440f), new VoiceParameters(1f, 1f, 1f)));
        var wobbly = FrequencyRange(
            Run(new VoiceShifter(2048), Sine(440f), new VoiceParameters(1f, 1f, 1f, Wobble: 1f))
        );

        // full wobble is +/- 0.8 semitones, about +/- 20 Hz at 440 Hz
        await Assert.That(steady).IsLessThan(4f);
        await Assert.That(wobbly).IsGreaterThan(20f);
    }

    [Test]
    public async Task Breathiness_AddsNoiseBetweenTheHarmonics()
    {
        var clean = Spectrum(Run(new VoiceShifter(2048), Vowel(150f, 1000f), new VoiceParameters(1f, 1f, 1f)));
        var breathy = Spectrum(
            Run(new VoiceShifter(2048), Vowel(150f, 1000f), new VoiceParameters(1f, 1f, 1f, Breathiness: 1f))
        );

        await Assert.That(BetweenHarmonics(breathy, 150f)).IsGreaterThan(4f * BetweenHarmonics(clean, 150f));
    }

    [Test]
    public async Task Breathiness_KeepsSilenceSilent()
    {
        var output = Run(new VoiceShifter(2048), _ => 0f, new VoiceParameters(1f, 1f, 1f, Breathiness: 1f));

        await Assert.That(output.Max(Math.Abs)).IsLessThan(1e-6f);
    }

    [Test]
    public async Task Whisper_HasNoHarmonics()
    {
        var voiced = Spectrum(Run(new VoiceShifter(2048), Vowel(150f, 1000f), new VoiceParameters(1f, 1f, 1f)));
        var whispered = Spectrum(
            Run(
                new VoiceShifter(2048),
                Vowel(150f, 1000f),
                new VoiceParameters(1f, 1f, 1f, Texture: VoiceTexture.Whisper)
            )
        );

        await Assert.That(HarmonicContrast(voiced, 150f)).IsGreaterThan(10f);
        await Assert.That(HarmonicContrast(whispered, 150f)).IsLessThan(3f);
    }

    [Test]
    public async Task Whisper_EnvelopeDoesNotRipple()
    {
        // a hard cepstral cutoff rings as a regular ripple across the envelope, which makes the shaped noise sound tonal
        var output = new float[BlockSize * 800];
        var vowel = Vowel(120f, 700f);
        var second = Vowel(120f, 2400f);
        for (var i = 0; i < output.Length; i++)
        {
            output[i] = vowel(i) + (0.5f * second(i));
        }

        var shifter = new VoiceShifter(2048);
        var parameters = new VoiceParameters(1f, 1f, 1f, Texture: VoiceTexture.Whisper);
        for (var start = 0; start < output.Length; start += BlockSize)
        {
            shifter.Process(parameters, 1024, 4, SampleRate, output.AsSpan(start, BlockSize));
        }

        await Assert.That(RippleDecibels(output, 300f, 1200f)).IsLessThan(2f);
    }

    [Test]
    public async Task Robot_PlaysHarmonicsOfItsNote()
    {
        var output = Run(
            new VoiceShifter(2048),
            Vowel(150f, 1000f),
            new VoiceParameters(1f, 1f, 1f, Texture: VoiceTexture.Robot)
        );

        // the strongest harmonic is one of 110 Hz near the formant, not one of the input's 150 Hz
        var peak = PeakFrequency(Spectrum(output));
        await Assert.That(peak).IsEqualTo(1000f).Within(200f);
        await Assert.That(DistanceToHarmonic(peak, VoiceShifter.RobotFrequency)).IsLessThan(6f);
        await Assert.That(DistanceToHarmonic(peak, 150f)).IsGreaterThan(20f);
    }

    /// <summary>
    /// The strongest regular ripple, in dB, with a period from <paramref name="minPeriod"/> to <paramref name="maxPeriod"/> Hz in the
    /// long-term log spectrum of the second half of <paramref name="signal"/> from 200 Hz to 8 kHz, after removing its shape over 1.5 kHz.
    /// </summary>
    private static float RippleDecibels(float[] signal, float minPeriod, float maxPeriod)
    {
        const int Size = 8192;
        var power = new double[Size / 2];
        for (var end = signal.Length; end - Size >= signal.Length / 2; end -= Size / 2)
        {
            var part = Spectrum(signal[..end]);
            for (var k = 0; k < power.Length; k++)
            {
                power[k] += part[k] * part[k];
            }
        }

        var log = power.Select(p => Math.Log(p + 1e-20)).ToArray();
        var from = (int)(200 * Size / SampleRate);
        var to = (int)(8000 * Size / SampleRate);
        var half = (int)(750 * Size / SampleRate);
        var ripple = new double[to - from];
        for (var k = from; k < to; k++)
        {
            var mean = 0.0;
            for (var j = k - half; j <= k + half; j++)
            {
                mean += log[Math.Clamp(j, 0, log.Length - 1)];
            }

            ripple[k - from] = log[k] - (mean / ((2 * half) + 1));
        }

        var strongest = 0.0;
        for (var period = minPeriod; period <= maxPeriod; period += 5f)
        {
            double re = 0;
            double im = 0;
            for (var i = 0; i < ripple.Length; i++)
            {
                var angle = 2 * Math.PI * (from + i) * SampleRate / Size / period;
                re += ripple[i] * Math.Cos(angle);
                im += ripple[i] * Math.Sin(angle);
            }

            strongest = Math.Max(strongest, 2 * Math.Sqrt((re * re) + (im * im)) / ripple.Length);
        }

        // natural log of power to dB
        return (float)(10 * strongest / Math.Log(10));
    }

    /// <summary>
    /// The average magnitude halfway between the harmonics of <paramref name="f0"/> from 1 to 4 kHz.
    /// </summary>
    private static float BetweenHarmonics(float[] spectrum, float f0)
    {
        var sum = 0f;
        var count = 0;
        for (var frequency = (MathF.Ceiling(1000f / f0) + 0.5f) * f0; frequency < 4000f; frequency += f0)
        {
            sum += MagnitudeAt(spectrum, frequency);
            count++;
        }

        return sum / count;
    }

    /// <summary>
    /// The range of the frequency of a sine over 1024-sample windows after the first half second, from its zero crossings.
    /// </summary>
    private static float FrequencyRange(float[] signal)
    {
        var min = float.MaxValue;
        var max = float.MinValue;
        for (var start = 24000; start + 1024 <= signal.Length; start += 1024)
        {
            double first = -1;
            double last = -1;
            var crossings = 0;
            for (var i = start + 1; i < start + 1024; i++)
            {
                if (signal[i - 1] < 0f && signal[i] >= 0f)
                {
                    var position = i - 1 + (signal[i - 1] / (signal[i - 1] - signal[i]));
                    if (first < 0)
                    {
                        first = position;
                    }

                    last = position;
                    crossings++;
                }
            }

            var frequency = (float)((crossings - 1) * SampleRate / (last - first));
            min = Math.Min(min, frequency);
            max = Math.Max(max, frequency);
        }

        return max - min;
    }

    /// <summary>
    /// How far the harmonics of <paramref name="f0"/> from 1 to 4 kHz stand out from what's between them.
    /// </summary>
    private static float HarmonicContrast(float[] spectrum, float f0)
    {
        var harmonics = 0f;
        var count = 0;
        for (var frequency = MathF.Ceiling(1000f / f0) * f0; frequency < 4000f; frequency += f0)
        {
            harmonics += MagnitudeAt(spectrum, frequency);
            count++;
        }

        return harmonics / count / BetweenHarmonics(spectrum, f0);
    }

    private static float[] Run(
        VoiceShifter shifter,
        Func<int, float> input,
        VoiceParameters parameters,
        int oversampling = 4
    )
    {
        var output = new float[BlockSize * 100];
        for (var i = 0; i < output.Length; i++)
        {
            output[i] = input(i);
        }

        for (var start = 0; start < output.Length; start += BlockSize)
        {
            shifter.Process(parameters, 1024, oversampling, SampleRate, output.AsSpan(start, BlockSize));
        }

        return output;
    }
}
