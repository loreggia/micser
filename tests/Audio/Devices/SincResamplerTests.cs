using Micser.Audio.Devices;
using NAudio.Dsp;

namespace Micser.Audio.Tests.Devices;

public class SincResamplerTests
{
    [Test]
    [Arguments(1, false)]
    [Arguments(2, false)]
    [Arguments(2, true)]
    [Arguments(6, true)]
    public async Task Resample_MatchesNAudiosWdlResampler(int channels, bool inputDriven)
    {
        var resampler = new SincResampler(channels, inputDriven);
        var reference = new WdlResampler();
        reference.SetMode(true, 0, true);
        reference.SetFilterParms();
        reference.SetFeedMode(inputDriven);
        var output = new float[600 * channels];
        var referenceOutput = new float[600 * channels];
        var maxDifference = 0f;
        var mismatches = 0;
        var totalFrames = 0;
        long phase = 0;

        for (var block = 0; block < 200; block++)
        {
            // rates around 44.1 -> 48 kHz and back, drifting like the drift correction does
            var correction = 1 + (0.004 * Math.Sin(block / 10.0));
            var (rateIn, rateOut) = block < 100 ? (44100 * correction, 48000.0) : (48000.0, 44100 / correction);
            resampler.SetRates(rateIn, rateOut);
            reference.SetRates(rateIn, rateOut);

            // every 7th block delivers less than requested, which flushes
            var requested = resampler.Prepare(240, out var input);
            var referenceRequested = reference.ResamplePrepare(240, channels, out var referenceInput);
            mismatches += requested == referenceRequested ? 0 : 1;
            var frames = block % 7 == 6 ? requested / 2 : requested;
            for (var i = 0; i < frames; i++, phase++)
            {
                for (var c = 0; c < channels; c++)
                {
                    input[(i * channels) + c] = referenceInput[(i * channels) + c] = (float)
                        Math.Sin(phase * 0.05 * (c + 1));
                }
            }

            var produced = resampler.Resample(output, frames, 500);
            var referenceProduced = reference.ResampleOut(referenceOutput, frames, 500, channels);
            mismatches += produced == referenceProduced ? 0 : 1;
            for (var i = 0; i < produced * channels; i++)
            {
                maxDifference = Math.Max(maxDifference, Math.Abs(output[i] - referenceOutput[i]));
            }

            totalFrames += produced;
        }

        await Assert.That(mismatches).IsEqualTo(0);
        await Assert.That(totalFrames).IsGreaterThan(200 * 200);
        await Assert.That(maxDifference).IsLessThan(1e-5f);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Resample_WithDriftingRates_DoesNotAllocateAfterWarmUp(bool inputDriven)
    {
        var resampler = new SincResampler(2, inputDriven);
        var output = new float[600 * 2];

        void Run()
        {
            for (var block = 0; block < 500; block++)
            {
                resampler.SetRates(44100 * (1 + (0.004 * Math.Sin(block / 10.0))), 48000);
                var requested = resampler.Prepare(240, out var input);
                input.Fill(0.1f);
                resampler.Resample(output, block % 7 == 6 ? requested / 2 : requested, 500);
            }
        }

        Run();
        var allocated = Allocations.Measure(Run);

        await Assert.That(allocated).IsEqualTo(0);
    }
}
