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
    public async Task WithoutBands_PassesSignalThrough()
    {
        var bench = new SignalTestBench(1000, 0.5f, null, new EqualizerModule());

        var output = bench.Run(TimeSpan.FromMilliseconds(100));

        await Assert.That(SignalTestBench.Peak(output)).IsEqualTo(0.5f).Within(1e-4f);
    }
}
