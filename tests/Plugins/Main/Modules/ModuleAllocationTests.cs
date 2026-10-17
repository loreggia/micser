using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Micser.Audio;
using Micser.Audio.Devices;
using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

/// <summary>
/// The modules must not allocate on the audio thread once they have seen a layout and state (see <c>AudioHarness alloc</c> for a profile
/// with random states).
/// </summary>
public class ModuleAllocationTests
{
    private static readonly ChannelLayout[] Layouts =
    [
        ChannelLayout.Mono,
        ChannelLayout.Surround51,
        ChannelLayout.Stereo,
    ];

    // states that make the modules do their work, applied in turn after the default state
    private static readonly Dictionary<Type, object[]> States = new()
    {
        [typeof(DelayState)] = [new DelayState(0.01f, [new DelayChannel(0.002f)]), new DelayState(0.5f, [])],
        [typeof(EqualizerState)] =
        [
            new EqualizerState([
                new EqualizerBand(100f, 6f),
                new EqualizerBand(5000f, -3f, 2f, EqualizerBandType.HighShelf),
            ]),
            new EqualizerState([new EqualizerBand(1000f, 3f)], -6f),
        ],
        [typeof(FilterState)] =
        [
            new FilterState(FilterType.LowPass, 2000f, 48),
            new FilterState(FilterType.HighPass, 200f, 24, 2f),
        ],
        [typeof(PitchState)] = [new PitchState(0.5f, 10), new PitchState(-0.3f, 1)],
    };

    public static IEnumerable<string> ModuleTypes()
    {
        return GetDefinitions().Select(d => d.Type);
    }

    [Test]
    [MethodDataSource(nameof(ModuleTypes))]
    public async Task Process_AfterSeeingLayoutsAndStates_DoesNotAllocate(string type)
    {
        var definition = GetDefinitions().Single(d => d.Type == type);
        using var devices = new AudioDeviceService(new NullAudioBackend());
        using var module = definition.CreateModule(new Services(devices));
        var source = new SignalTestBench.SineSource(1000, 0.25f, ChannelLayout.Mono);
        var graph = new AudioGraph(SignalTestBench.Format) { TrackAllocations = true };
        graph.Add(source);
        graph.Add(module);
        foreach (var input in module.Inputs)
        {
            graph.Connect(source.Output, input);
        }

        object[] states = [definition.GetState(module), .. States.GetValueOrDefault(definition.StateType, [])];

        Run(graph, source, definition, module, states);
        var allocated = Allocations.Measure(
            () => Run(graph, source, definition, module, states),
            () => module.AllocatedBytes
        );

        await Assert.That(allocated).IsEqualTo(0);
    }

    private static AudioModuleDefinition[] GetDefinitions()
    {
        var services = new ServiceCollection();
        new MainPlugin().ConfigureServices(services);
        return [.. services.Select(s => s.ImplementationInstance).OfType<AudioModuleDefinition>()];
    }

    private static void Run(
        AudioGraph graph,
        SignalTestBench.SineSource source,
        AudioModuleDefinition definition,
        AudioModule module,
        object[] states
    )
    {
        foreach (var layout in Layouts)
        {
            source.Layout = layout;
            foreach (var state in states)
            {
                definition.SetState(module, state);
                module.Volume = module.Volume == 1f ? 0.5f : 1f;
                for (var i = 0; i < 20; i++)
                {
                    graph.Process();
                }
            }
        }
    }

    /// <summary>
    /// What the modules' constructors need: a device service without devices and loggers.
    /// </summary>
    private sealed class Services : IServiceProvider
    {
        private readonly AudioDeviceService _devices;

        public Services(AudioDeviceService devices)
        {
            _devices = devices;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(AudioDeviceService))
            {
                return _devices;
            }

            return serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(ILogger<>)
                ? Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(serviceType.GetGenericArguments()))
                : null;
        }
    }
}
