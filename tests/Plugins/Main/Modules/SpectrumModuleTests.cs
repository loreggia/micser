using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class SpectrumModuleTests
{
    [Test]
    public async Task GetSpectrum_BeforeAttach_ReturnsNull()
    {
        await Assert.That(new SpectrumModule().GetSpectrum()).IsNull();
    }

    [Test]
    public async Task GetSpectrum_ShowsSineAtItsBinWithItsAmplitude()
    {
        const int bin = 100;
        var spectrum = new SpectrumModule();
        var frequency = bin * (float)SignalTestBench.Format.SampleRate / SpectrumModule.FftSize;
        var bench = new SignalTestBench(frequency, 0.5f, ChannelLayout.Stereo, spectrum);
        bench.Run(TimeSpan.FromMilliseconds(200));

        var result = spectrum.GetSpectrum()!;

        await Assert.That(result.FrequencyResolution).IsEqualTo(frequency / bin).Within(1e-3f);
        await Assert.That(Array.IndexOf(result.Magnitudes, result.Magnitudes.Max())).IsEqualTo(bin);
        await Assert.That(result.Magnitudes[bin]).IsEqualTo(0.5f).Within(0.01f);
    }

    [Test]
    public async Task Process_PassesAudioThroughUnchanged()
    {
        var bench = new SignalTestBench(1000, 0.5f, null, new SpectrumModule());

        var output = bench.Run(TimeSpan.FromMilliseconds(100));

        await Assert.That(SignalTestBench.Peak(output)).IsEqualTo(0.5f).Within(1e-4f);
    }
}
