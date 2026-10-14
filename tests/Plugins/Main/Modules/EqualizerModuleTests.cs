using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class EqualizerModuleTests
{
    [Test]
    public async Task Band_BarelyAffectsDistantFrequencies()
    {
        var equalizer = new EqualizerModule { Bands = [new EqualizerBand(8000, 12f, 2f)] };
        var bench = new SignalTestBench(200, 0.1f, null, equalizer);

        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert
            .That(Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)) / 0.1f))
            .IsEqualTo(0f)
            .Within(0.5f);
    }

    [Test]
    public async Task Band_BoostsItsCenterFrequency()
    {
        var equalizer = new EqualizerModule { Bands = [new EqualizerBand(1000, 12f, 1f)] };
        var bench = new SignalTestBench(1000, 0.1f, null, equalizer);

        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert
            .That(Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)) / 0.1f))
            .IsEqualTo(12f)
            .Within(0.2f);
    }

    [Test]
    public async Task ChangingBandGain_TakesEffect()
    {
        var equalizer = new EqualizerModule { Bands = [new EqualizerBand(1000, 12f, 1f)] };
        var bench = new SignalTestBench(1000, 0.1f, null, equalizer);
        bench.Run(TimeSpan.FromMilliseconds(200));

        equalizer.Bands = [new EqualizerBand(1000, -12f, 1f)];
        var output = bench.Run(TimeSpan.FromMilliseconds(500));

        await Assert
            .That(Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)) / 0.1f))
            .IsEqualTo(-12f)
            .Within(0.2f);
    }

    [Test]
    [Arguments(EqualizerBandType.LowShelf, 100f, 6f)]
    [Arguments(EqualizerBandType.LowShelf, 10000f, 0f)]
    [Arguments(EqualizerBandType.HighShelf, 100f, 0f)]
    [Arguments(EqualizerBandType.HighShelf, 10000f, 6f)]
    public async Task Shelf_ChangesOneSideOfItsFrequency(EqualizerBandType type, float frequency, float expected)
    {
        var equalizer = new EqualizerModule { Bands = [new EqualizerBand(1000, 6f, 0.707f, type)] };

        await Assert.That(GainAt(frequency, equalizer)).IsEqualTo(expected).Within(0.3f);
    }

    [Test]
    [Arguments(EqualizerBandType.LowPass, 100f, 0f)]
    [Arguments(EqualizerBandType.LowPass, 1000f, -3f)]
    [Arguments(EqualizerBandType.HighPass, 1000f, -3f)]
    [Arguments(EqualizerBandType.HighPass, 10000f, 0f)]
    public async Task Pass_FollowsItsSlope(EqualizerBandType type, float frequency, float expected)
    {
        // the gain doesn't apply to low-pass and high-pass bands
        var equalizer = new EqualizerModule { Bands = [new EqualizerBand(1000, 12f, 0.707f, type)] };

        await Assert.That(GainAt(frequency, equalizer)).IsEqualTo(expected).Within(0.5f);
    }

    [Test]
    [Arguments(EqualizerBandType.LowPass, 8000f)]
    [Arguments(EqualizerBandType.HighPass, 125f)]
    public async Task Pass_RemovesThreeOctavesAway(EqualizerBandType type, float frequency)
    {
        var equalizer = new EqualizerModule { Bands = [new EqualizerBand(1000, 0f, 0.707f, type)] };

        // 12 dB per octave
        await Assert.That(GainAt(frequency, equalizer)).IsLessThan(-34f);
    }

    [Test]
    public async Task Preamp_ChangesTheLevelBeforeTheBands()
    {
        var equalizer = new EqualizerModule { Preamp = -6f, Bands = [new EqualizerBand(1000, 6f, 1f)] };

        await Assert.That(GainAt(1000, equalizer)).IsEqualTo(0f).Within(0.2f);
        await Assert.That(GainAt(100, new EqualizerModule { Preamp = -6f })).IsEqualTo(-6f).Within(0.01f);
    }

    [Test]
    public async Task SetState_RoundTripsTypesAndPreamp()
    {
        var state = new EqualizerState([new EqualizerBand(80, 4f, 0.7f, EqualizerBandType.LowShelf)], -3f);
        var equalizer = new EqualizerModule();

        equalizer.SetState(state);

        await Assert.That(equalizer.GetState().Preamp).IsEqualTo(-3f);
        await Assert.That(equalizer.GetState().Bands).IsEquivalentTo(state.Bands);
    }

    [Test]
    public async Task WithoutBands_PassesSignalThrough()
    {
        var bench = new SignalTestBench(1000, 0.5f, null, new EqualizerModule());

        var output = bench.Run(TimeSpan.FromMilliseconds(100));

        await Assert.That(SignalTestBench.Peak(output)).IsEqualTo(0.5f).Within(1e-4f);
    }

    private static float GainAt(float frequency, EqualizerModule equalizer)
    {
        var output = new SignalTestBench(frequency, 0.1f, null, equalizer).Run(TimeSpan.FromMilliseconds(500));
        return Decibels.FromLinear(SignalTestBench.Peak(output.AsSpan(output.Length - 4800)) / 0.1f);
    }
}
