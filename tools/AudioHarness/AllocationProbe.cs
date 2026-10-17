using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Micser.Audio;
using Micser.Audio.Devices;
using Micser.Plugins.Main;

namespace Micser.AudioHarness;

/// <summary>
/// Runs every module type of the Main plugin in one graph without a clock or devices and reports what each allocates on the audio thread
/// (<see cref="AudioGraph.TrackAllocations"/>), in three situations:
/// <list type="bullet">
/// <item>steady: a fixed layout and state; anything here allocates on every block or now and then;</item>
/// <item>layouts: the blocks right after the source switches to another layout;</item>
/// <item>changes: the blocks right after a random state, volume, mute, bypass or channel count change.</item>
/// The whole sequence runs twice with the same random states, and only the second run counts: allocating the first time a module sees a
/// layout or state is fine, allocating again when it comes back isn't.
/// </list>
/// Each module gets the source directly on every input, so it sees all layouts; its outputs go to a sink, whole and channel by channel,
/// whose row shows the mixing of many connections.
/// </summary>
internal static class AllocationProbe
{
    private static readonly ChannelLayout[] Layouts =
    [
        ChannelLayout.Mono,
        ChannelLayout.Stereo,
        ChannelLayout.Surround51,
        ChannelLayout.Surround71,
        ChannelLayout.Quad,
        ChannelLayout.FromChannelCount(3),
    ];

    /// <param name="types">The module types to run, or empty for all.</param>
    /// <param name="blocks">Blocks per steady phase.</param>
    /// <param name="showTypes">Also lists the allocated types (sampled every ~100 KB by the runtime, so it needs enough blocks).</param>
    /// <returns>Whether nothing allocated in the steady phases.</returns>
    public static bool Run(
        AudioDeviceService devices,
        IReadOnlyCollection<string> types,
        int blocks,
        int seed,
        bool showTypes
    )
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(devices);
        new MainPlugin().ConfigureServices(services);
        using var provider = services.BuildServiceProvider();

        var definitions = provider
            .GetServices<AudioModuleDefinition>()
            .Where(d => types.Count == 0 || types.Contains(d.Type, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (definitions.Length == 0)
        {
            Console.Error.WriteLine("No such module type.");
            return false;
        }

        var graph = new AudioGraph(ProcessingFormat.Default) { TrackAllocations = true };
        var source = new SineSource();
        var sink = new Sink();
        graph.Add(source);
        graph.Add(sink);

        var modules = new List<(string Name, AudioModule Module)>();
        foreach (var definition in definitions)
        {
            var module = definition.CreateModule(provider);
            graph.Add(module);
            foreach (var input in module.Inputs)
            {
                graph.Connect(source.Output, input);
            }

            foreach (var output in module.Outputs)
            {
                graph.Connect(output, sink.Input);
                graph.Connect(output, sink.Input, 1, 0);
                graph.Connect(output, sink.Input, 0, 2);
            }

            modules.Add((definition.Type, module));
        }

        modules.Add(("(sink)", sink));
        var results = modules.ToDictionary(m => m.Module, _ => new long[3]);
        using var listener = showTypes ? new AllocationListener() : null;

        // The second pass repeats the first, so it only counts what allocates again on a layout or state that was seen before, not e.g.
        // channel mixers created for a new combination of layouts.
        for (var pass = 0; pass < 2; pass++)
        {
            var random = new Random(seed);
            foreach (var layout in Layouts)
            {
                source.Layout = layout;
                RandomizeAll(random);
                Measure(graph, modules, results, 4, pass == 1 ? Phase.Layouts : null);
                Measure(graph, modules, results, blocks, pass == 1 ? Phase.Steady : null);

                for (var change = 0; change < 8; change++)
                {
                    RandomizeAll(random);
                    Measure(graph, modules, results, 2, pass == 1 ? Phase.Changes : null);
                    Measure(graph, modules, results, blocks / 8, pass == 1 ? Phase.Steady : null);
                }
            }
        }

        void RandomizeAll(Random random)
        {
            foreach (var (definition, (_, module)) in definitions.Zip(modules))
            {
                Randomize(definition, module, random);
            }
        }

        Console.WriteLine($"{"Module", -16} {"steady", 12} {"layouts", 12} {"changes", 12}");
        foreach (var (name, module) in modules)
        {
            var result = results[module];
            Console.WriteLine(
                $"{name, -16} {FormatBytes(result[(int)Phase.Steady]), 12} {FormatBytes(result[(int)Phase.Layouts]), 12} {FormatBytes(result[(int)Phase.Changes]), 12}"
            );
        }

        if (listener != null)
        {
            Console.WriteLine();
            Console.WriteLine("Allocated types (all threads, both passes, sampled):");
            foreach (var (type, bytes) in listener.Types.OrderByDescending(t => t.Value))
            {
                Console.WriteLine($"  {FormatBytes(bytes), 10}  {type}");
            }
        }

        return results.Values.All(r => r[(int)Phase.Steady] == 0);
    }

    private static string FormatBytes(long bytes)
    {
        return bytes == 0 ? "-" : $"{bytes:N0} B";
    }

    private static void Measure(
        AudioGraph graph,
        List<(string Name, AudioModule Module)> modules,
        Dictionary<AudioModule, long[]> results,
        int blocks,
        Phase? phase
    )
    {
        var before = modules.Select(m => m.Module.AllocatedBytes).ToArray();
        for (var i = 0; i < blocks; i++)
        {
            graph.Process();
        }

        if (phase is { } p)
        {
            for (var i = 0; i < modules.Count; i++)
            {
                results[modules[i].Module][(int)p] += modules[i].Module.AllocatedBytes - before[i];
            }
        }
    }

    /// <summary>
    /// Sets a random valid state (within the data annotations of the state record) and random module settings.
    /// </summary>
    private static void Randomize(AudioModuleDefinition definition, AudioModule module, Random random)
    {
        definition.SetState(module, CreateRandom(definition.StateType, random));
        module.Volume = random.Next(4) == 0 ? 1f : random.NextSingle();
        module.IsMuted = random.Next(8) == 0;
        module.ChannelCount = random.Next(4) == 0 ? random.Next(1, 9) : null;
        if (module is EffectModule effect)
        {
            effect.IsBypassed = random.Next(8) == 0;
        }
    }

    private static object CreateRandom(Type recordType, Random random)
    {
        var constructor = recordType
            .GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First(c => c.GetParameters().All(p => p.ParameterType != recordType));
        var arguments = constructor.GetParameters().Select(p => CreateRandom(p, random)).ToArray();
        return constructor.Invoke(arguments);
    }

    private static object? CreateRandom(ParameterInfo parameter, Random random)
    {
        var type = parameter.ParameterType;
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null && random.Next(4) == 0)
        {
            return null;
        }

        type = underlying ?? type;
        var range = parameter.GetCustomAttribute<RangeAttribute>();
        double Next(double min, double max)
        {
            var (lower, upper) =
                range != null ? (Convert.ToDouble(range.Minimum), Convert.ToDouble(range.Maximum)) : (min, max);
            return lower + (random.NextDouble() * (upper - lower));
        }

        if (type == typeof(float))
        {
            return (float)Next(-1, 1);
        }

        if (type == typeof(double))
        {
            return Next(-1, 1);
        }

        if (type == typeof(int))
        {
            return (int)Math.Round(Next(0, 10));
        }

        if (type == typeof(bool))
        {
            return random.Next(2) == 0;
        }

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(random.Next(values.Length));
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            var elementType = type.GetGenericArguments()[0];
            var maxLength = Math.Min(parameter.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? 8, 8);
            var minLength = parameter.GetCustomAttribute<MinLengthAttribute>()?.Length ?? 0;
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
            for (var i = random.Next(minLength, maxLength + 1); i > 0; i--)
            {
                list.Add(CreateRandom(elementType, random));
            }

            return list;
        }

        // strings (e.g. device IDs) and other references keep their default, so no device is opened
        return parameter.HasDefaultValue ? parameter.DefaultValue : null;
    }

    private sealed class AllocationListener : EventListener
    {
        private const EventKeywords GcKeyword = (EventKeywords)0x1;

        public Dictionary<string, long> Types { get; } = [];

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-Windows-DotNETRuntime")
            {
                EnableEvents(eventSource, EventLevel.Verbose, GcKeyword);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName?.StartsWith("GCAllocationTick", StringComparison.Ordinal) != true)
            {
                return;
            }

            var names = eventData.PayloadNames!;
            var type = eventData.Payload![names.IndexOf("TypeName")] as string ?? "?";
            var amount = Convert.ToInt64(eventData.Payload[names.IndexOf("AllocationAmount64")]);
            lock (Types)
            {
                Types[type] = Types.GetValueOrDefault(type) + amount;
            }
        }
    }

    private enum Phase
    {
        Steady,
        Layouts,
        Changes,
    }

    private sealed class Sink : AudioModule
    {
        public Sink()
        {
            Input = AddInput("Input");
        }

        public InputPort Input { get; }

        protected override void Process() { }
    }

    private sealed class SineSource : AudioModule
    {
        private double _phase;

        public SineSource()
        {
            Output = AddOutput("Output");
        }

        public ChannelLayout Layout { get; set; } = ChannelLayout.Stereo;

        public OutputPort Output { get; }

        protected override void Process()
        {
            var buffer = Output.Buffer;
            buffer.SetLayout(Layout);
            var increment = 2 * Math.PI * 440 / Format.SampleRate;
            for (var c = 0; c < buffer.ChannelCount; c++)
            {
                var samples = buffer.GetChannel(c);
                var phase = _phase;
                for (var i = 0; i < samples.Length; i++)
                {
                    samples[i] = 0.25f * (float)Math.Sin(phase * (c + 1));
                    phase += increment;
                }
            }

            _phase += increment * buffer.FrameCount;
        }
    }
}
