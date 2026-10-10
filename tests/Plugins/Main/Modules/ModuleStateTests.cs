using Micser.Audio;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class ModuleStateTests
{
    [Test]
    public async Task Definitions_HaveUniqueTypesAndStates()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        new MainPlugin().ConfigureServices(services);
        var definitions = services.Select(d => d.ImplementationInstance).OfType<AudioModuleDefinition>().ToArray();

        await Assert.That(definitions).Count().IsEqualTo(9);
        await Assert.That(definitions.Select(d => d.Type).Distinct()).Count().IsEqualTo(9);
        await Assert.That(definitions.Select(d => d.StateType).Distinct()).Count().IsEqualTo(9);
    }

    [Test]
    public async Task NewModules_MatchTheirStateDefaults()
    {
        await Assert.That(new GainModule().GetState()).IsEqualTo(new GainState());
        await Assert.That(new CompressorModule().GetState()).IsEqualTo(new CompressorState());
        await Assert.That(new FilterModule().GetState()).IsEqualTo(new FilterState());
        await Assert.That(new PitchModule().GetState()).IsEqualTo(new PitchState());
    }

    [Test]
    public async Task SetState_RoundTrips()
    {
        var compressor = new CompressorModule();
        var state = new CompressorState(CompressorType.Upward, 0.5f, 0.02f, 0.3f, 4f, -30f, 2f, 3f);
        var equalizer = new EqualizerModule();
        var bands = new EqualizerState([new EqualizerBand(100, 3f), new EqualizerBand(1000, -6f, 2f)]);

        compressor.SetState(state);
        equalizer.SetState(bands);

        await Assert.That(compressor.GetState()).IsEqualTo(state);
        await Assert.That(equalizer.GetState().Bands).IsEquivalentTo(bands.Bands);
    }
}
