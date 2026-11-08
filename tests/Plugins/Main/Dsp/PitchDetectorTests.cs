using Micser.Plugins.Main.Dsp;
using static Micser.Plugins.Main.Tests.Dsp.TestSignals;

namespace Micser.Plugins.Main.Tests.Dsp;

public class PitchDetectorTests
{
    [Test]
    [Arguments(48000, 2048)]
    [Arguments(44100, 2048)]
    [Arguments(96000, 4096)]
    public async Task GetSize_HoldsTheLongestPeriodInItsHalf(int sampleRate, int size)
    {
        await Assert.That(PitchDetector.GetSize(sampleRate)).IsEqualTo(size);
        await Assert.That(size / 2).IsGreaterThanOrEqualTo((int)(sampleRate / PitchDetector.MinFrequency));
    }

    [Test]
    [Arguments(65f)]
    [Arguments(110f)]
    [Arguments(220f)]
    [Arguments(440f)]
    [Arguments(900f)]
    public async Task Detect_Sine_FindsItsFrequency(float frequency)
    {
        var detected = Detect(Sine(frequency), 48000);

        await Assert.That(detected).IsEqualTo(frequency).Within(frequency * 0.005f);
    }

    [Test]
    [Arguments(100f, 1000f)]
    [Arguments(150f, 700f)]
    [Arguments(250f, 2500f)]
    public async Task Detect_Vowel_FindsItsFundamental(float f0, float formant)
    {
        // the strongest harmonic is near the formant, not the fundamental
        var detected = Detect(Vowel(f0, formant), 48000);

        await Assert.That(detected).IsEqualTo(f0).Within(f0 * 0.005f);
    }

    [Test]
    public async Task Detect_At96Kilohertz_FindsTheFrequency()
    {
        var sine = Sine(100f);

        // the test signals are at 48 kHz: twice the samples per period at twice the rate is the same frequency
        var detected = Detect(n => sine(n / 2), 96000);

        await Assert.That(detected).IsEqualTo(100f).Within(0.5f);
    }

    [Test]
    public async Task Detect_Silence_IsZero()
    {
        await Assert.That(Detect(_ => 0f, 48000)).IsEqualTo(0f);
    }

    [Test]
    public async Task Detect_Noise_IsZero()
    {
        var random = new Random(5);

        await Assert.That(Detect(_ => (float)(random.NextDouble() - 0.5), 48000)).IsEqualTo(0f);
    }

    private static float Detect(Func<int, float> input, int sampleRate)
    {
        var detector = new PitchDetector(96000);
        for (var n = 0; n < 8192; n++)
        {
            detector.Write(input(n));
        }

        return detector.Detect(sampleRate);
    }
}
