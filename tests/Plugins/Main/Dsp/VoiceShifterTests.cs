using Micser.Plugins.Main.Dsp;

namespace Micser.Plugins.Main.Tests.Dsp;

public class VoiceShifterTests
{
    private const int BlockSize = 480;
    private const float SampleRate = 48000f;

    [Test]
    public async Task FormantsWithPitch_OneOctaveUp_DoublesFrequency()
    {
        var output = Run(new VoiceShifter(2048), Sine(440f), 2f, 2f, 1f);

        await Assert.That(PeakFrequency(output)).IsEqualTo(880f).Within(10f);
    }

    [Test]
    public async Task Formant_Up_MovesTheFormantAndKeepsThePitch()
    {
        var factor = MathF.Pow(2f, 5f / 12f);

        var output = Run(new VoiceShifter(2048), Vowel(150f, 1000f), 1f, factor, 1f);

        // the strongest harmonic is still a multiple of 150 Hz, but near the moved formant
        var peak = PeakFrequency(output);
        await Assert.That(peak).IsEqualTo(1000f * factor).Within(150f);
        await Assert.That(DistanceToHarmonic(peak, 150f)).IsLessThan(10f);
    }

    [Test]
    public async Task Pitch_Up_KeepsTheFormant()
    {
        var factor = MathF.Pow(2f, 5f / 12f);

        var output = Run(new VoiceShifter(2048), Vowel(150f, 1000f), factor, 1f, 1f);

        // the harmonics are multiples of the new pitch, the strongest still near the formant
        var peak = PeakFrequency(output);
        await Assert.That(peak).IsEqualTo(1000f).Within(150f);
        await Assert.That(DistanceToHarmonic(peak, 150f * factor)).IsLessThan(10f);
    }

    [Test]
    public async Task Mix_Zero_DelaysTheDrySignalByTheLatency()
    {
        var input = Sine(440f);
        var output = Run(new VoiceShifter(2048), input, 2f, 2f, 0f);
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
        var used = new VoiceShifter(2048);
        Run(used, Sine(300f), 1.5f, 0.8f, 1f);
        used.Reset();

        var afterReset = Run(used, Vowel(150f, 1000f), 1.2f, 1.1f, 1f);
        var fresh = Run(new VoiceShifter(2048), Vowel(150f, 1000f), 1.2f, 1.1f, 1f);

        await Assert.That(afterReset).IsEquivalentTo(fresh);
    }

    [Test]
    public async Task OversamplingChange_ProcessesLikeANewInstance()
    {
        var used = new VoiceShifter(2048);
        Run(used, Sine(300f), 1.5f, 0.8f, 1f);

        var changed = Run(used, Vowel(150f, 1000f), 1.2f, 1.1f, 1f, 8);
        var fresh = Run(new VoiceShifter(2048), Vowel(150f, 1000f), 1.2f, 1.1f, 1f, 8);

        await Assert.That(changed).IsEquivalentTo(fresh);
    }

    private static float DistanceToHarmonic(float frequency, float f0)
    {
        return Math.Abs(frequency - (f0 * MathF.Round(frequency / f0)));
    }

    /// <summary>
    /// The frequency of the strongest component of the last 8192 samples.
    /// </summary>
    private static float PeakFrequency(float[] signal)
    {
        const int Size = 8192;
        var window = new float[Size];
        HannWindow.Fill(window);
        var data = new float[2 * Size];
        for (var n = 0; n < Size; n++)
        {
            data[2 * n] = signal[signal.Length - Size + n] * window[n];
        }

        new Fft(Size).Transform(data, Size, inverse: false);
        var peak = 1;
        var peakMagnitude = 0f;
        for (var k = 1; k < Size / 2; k++)
        {
            var magnitude = (data[2 * k] * data[2 * k]) + (data[(2 * k) + 1] * data[(2 * k) + 1]);
            if (magnitude > peakMagnitude)
            {
                (peak, peakMagnitude) = (k, magnitude);
            }
        }

        return peak * SampleRate / Size;
    }

    private static float[] Run(
        VoiceShifter shifter,
        Func<int, float> input,
        float pitch,
        float formant,
        float mix,
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
            shifter.Process(pitch, formant, mix, 1024, oversampling, SampleRate, output.AsSpan(start, BlockSize));
        }

        return output;
    }

    private static Func<int, float> Sine(float frequency)
    {
        return n => 0.5f * MathF.Sin(2 * MathF.PI * frequency * n / SampleRate);
    }

    /// <summary>
    /// Harmonics of <paramref name="f0"/> up to 8 kHz, shaped by a single formant around <paramref name="formant"/>.
    /// </summary>
    private static Func<int, float> Vowel(float f0, float formant)
    {
        return n =>
        {
            var sample = 0.0;
            for (var h = 1; h * f0 < 8000f; h++)
            {
                var frequency = h * f0;
                var distance = (frequency - formant) / 300.0;
                var amplitude = (1.0 / (1.0 + (distance * distance))) + 0.02;
                sample += 0.05 * amplitude * Math.Sin(2 * Math.PI * frequency * n / SampleRate);
            }

            return (float)sample;
        };
    }
}
