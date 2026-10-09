using Microsoft.Extensions.Logging;
using Micser.Audio;
using Micser.Audio.Devices;
using Micser.AudioHarness;
using Micser.Plugins.Main.Modules;
using Serilog;
using Serilog.Extensions.Logging;

// Routes a real input through a gain module to a real output and prints buffer statistics once per second.
//
//   Micser.AudioHarness list
//   Micser.AudioHarness <input> <output> [--gain <dB>] [--loopback]
//   Micser.AudioHarness latency <output> [<input>] [--seconds <n>]
//   Micser.AudioHarness formats <output> <input>
//
// <input>/<output> are a device number from "list", a device ID, part of a device name or "default". With --loopback, <input> is an output
// device whose playback is captured.
//
// "latency" plays a quiet noise burst (-40 dB) on <output> twice per second, captures it again through the loopback of
// the same device and prints the round trip: render buffering + capture buffering, without any hardware latency.
// With <input>, it captures from that input instead, e.g. the output side of a virtual cable whose input is <output>.
// --seconds stops the measurement after that time instead of at Ctrl+C.
// "formats" lists which stream formats <output> and <input> accept (shared mode with and without the audio engine's conversion, and
// exclusive mode), then plays a 1 kHz tone at -20 dBFS into <output> in a few formats and measures it at <input>, e.g. the two sides of a
// virtual cable. Finally it plays a different tone per channel and prints where each one arrives at which level (channel maps).
// The environment variable MICSER_BLOCK overrides the engine block size (frames) for this measurement.

Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
using var loggerFactory = new SerilogLoggerFactory(Log.Logger);
using var devices = new AudioDeviceService(loggerFactory.CreateLogger<AudioDeviceService>());

var inputs = devices.GetDevices(DeviceDirection.Input);
var outputs = devices.GetDevices(DeviceDirection.Output);

if (args.Length == 0 || args[0] == "list")
{
    PrintDevices("Inputs", inputs);
    PrintDevices("Outputs", outputs);
    return 0;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

if (args[0] == "latency")
{
    var secondsIndex = Array.IndexOf(args, "--seconds");
    if (secondsIndex >= 0 && secondsIndex < args.Length - 1)
    {
        cancellation.CancelAfter(TimeSpan.FromSeconds(int.Parse(args[secondsIndex + 1])));
    }

    var positional =
        secondsIndex < 0 ? args : args.Where((_, i) => i != secondsIndex && i != secondsIndex + 1).ToArray();
    return await MeasureLatencyAsync(
        positional.Length > 1 ? positional[1] : "default",
        positional.Length > 2 ? positional[2] : null
    );
}

if (args[0] == "formats")
{
    var render = ResolveDevice(args.Length > 1 ? args[1] : "default", DeviceDirection.Output, outputs);
    var capture = ResolveDevice(args.Length > 2 ? args[2] : "default", DeviceDirection.Input, inputs);
    if (render == null || capture == null)
    {
        Console.Error.WriteLine("Unknown device. Run with \"list\" to see the available devices.");
        return 1;
    }

    FormatProbe.Run(render.Id, capture.Id);
    return 0;
}

var loopback = args.Contains("--loopback");
var gainIndex = Array.IndexOf(args, "--gain");
var gain =
    gainIndex >= 0 && gainIndex < args.Length - 1
        ? float.Parse(args[gainIndex + 1], System.Globalization.CultureInfo.InvariantCulture)
        : 0f;

var input = ResolveDevice(
    args[0],
    loopback ? DeviceDirection.Output : DeviceDirection.Input,
    loopback ? outputs : inputs
);
var output = args.Length > 1 ? ResolveDevice(args[1], DeviceDirection.Output, outputs) : null;
if (input == null || output == null)
{
    Console.Error.WriteLine("Unknown device. Run with \"list\" to see the available devices.");
    return 1;
}

var graph = new AudioGraph(ProcessingFormat.Default, loggerFactory.CreateLogger<AudioGraph>());
using CaptureModule inputModule = loopback
    ? new LoopbackInputModule(devices, loggerFactory.CreateLogger<LoopbackInputModule>())
    : new DeviceInputModule(devices, loggerFactory.CreateLogger<DeviceInputModule>());
using var gainModule = new GainModule { Gain = gain };
using var outputModule = new DeviceOutputModule(devices, loggerFactory.CreateLogger<DeviceOutputModule>());

inputModule.SelectDevice(input.Id);
outputModule.SelectDevice(output.Id);
graph.Add(inputModule);
graph.Add(gainModule);
graph.Add(outputModule);
graph.Connect(inputModule.Output, gainModule.Input);
graph.Connect(gainModule.Output, outputModule.Input);

using var engine = new AudioEngine(graph, loggerFactory.CreateLogger<AudioEngine>());
engine.Start();

Console.WriteLine(
    $"{input.Name} ({input.Layout}, {input.SampleRate} Hz) -> gain {gain:+0.#;-0.#;0} dB -> {output.Name} ({output.Layout}, {output.SampleRate} Hz)"
);
Console.WriteLine("Press Ctrl+C to stop.");

try
{
    while (!cancellation.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), cancellation.Token);
        var stats = engine.Statistics;
        engine.ResetMaxProcessingTime();
        Console.WriteLine(
            $"blocks {stats.Blocks, 6} late {stats.LateBlocks, 3} max {stats.MaxProcessingTime.TotalMilliseconds, 5:0.00} ms | "
                + $"in {Format(inputModule.Statistics)} | out {Format(outputModule.Statistics)}"
        );
    }
}
catch (OperationCanceledException) { }

engine.Stop();
return 0;

static string Format(StreamStatistics? statistics)
{
    return statistics is { } s
        ? $"fill {s.Fill, 6:0}/{s.TargetFill:0} corr {(s.Correction - 1) * 1e6, 6:+0;-0;0} ppm under {s.Underruns, 3} over {s.Overruns, 3}"
        : "no device";
}

static void PrintDevices(string title, IReadOnlyList<AudioDeviceInfo> devices)
{
    Console.WriteLine(title);
    for (var i = 0; i < devices.Count; i++)
    {
        var device = devices[i];
        Console.WriteLine($"  {i + 1, 2}. {device.Name} [{device.Layout}, {device.SampleRate} Hz]");
        Console.WriteLine($"      {device.Id}");
    }
}

async Task<int> MeasureLatencyAsync(string outputArgument, string? inputArgument)
{
    var device = ResolveDevice(outputArgument, DeviceDirection.Output, outputs);
    var inputDevice = inputArgument != null ? ResolveDevice(inputArgument, DeviceDirection.Input, inputs) : null;
    if (device == null || (inputArgument != null && inputDevice == null))
    {
        Console.Error.WriteLine("Unknown device. Run with \"list\" to see the available devices.");
        return 1;
    }

    var format = new ProcessingFormat(
        48000,
        int.Parse(Environment.GetEnvironmentVariable("MICSER_BLOCK") ?? ProcessingFormat.Default.FrameCount.ToString())
    );
    var latencyGraph = new AudioGraph(format, loggerFactory.CreateLogger<AudioGraph>());
    using var source = new NoiseBurstSource(format.SampleRate / 2);
    using var detector = new NoiseBurstDetector(source, format.SampleRate * 2 / 5);
    using var render = new DeviceOutputModule(devices, loggerFactory.CreateLogger<DeviceOutputModule>());
    using CaptureModule capture =
        inputDevice != null
            ? new DeviceInputModule(devices, loggerFactory.CreateLogger<DeviceInputModule>())
            : new LoopbackInputModule(devices, loggerFactory.CreateLogger<LoopbackInputModule>());
    render.SelectDevice(device.Id);
    capture.SelectDevice(inputDevice?.Id ?? device.Id);
    latencyGraph.Add(source);
    latencyGraph.Add(render);
    latencyGraph.Add(capture);
    latencyGraph.Add(detector);
    latencyGraph.Connect(source.Output, render.Input);
    latencyGraph.Connect(capture.Output, detector.Input);

    using var latencyEngine = new AudioEngine(latencyGraph, loggerFactory.CreateLogger<AudioEngine>());
    latencyEngine.Start();
    Console.WriteLine(
        inputDevice != null
            ? $"Measuring {device.Name} -> {inputDevice.Name} ({inputDevice.Layout}, {inputDevice.SampleRate} Hz). Press Ctrl+C to stop."
            : $"Measuring {device.Name} ({device.Layout}, {device.SampleRate} Hz). Press Ctrl+C to stop."
    );

    var all = new List<double>();
    var missed = 0;
    try
    {
        while (!cancellation.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellation.Token);
            var measured = detector.TakeLatencies().Select(l => l * 1000d / format.SampleRate).ToArray();
            all.AddRange(measured.OfType<double>());
            missed += measured.Count(m => m == null);
            Console.WriteLine(
                $"round trip {string.Join(", ", measured.Select(m => m is { } ms ? $"{ms, 6:0.0} ms" : "  none   "))} | in {Format(capture.Statistics)} | out {Format(render.Statistics)}"
            );
        }
    }
    catch (OperationCanceledException) { }

    latencyEngine.Stop();
    if (all.Count > 0)
    {
        all.Sort();
        Console.WriteLine(
            $"min {all[0]:0.0} ms, median {all[all.Count / 2]:0.0} ms, max {all[^1]:0.0} ms ({all.Count} bursts, {missed} missed)"
        );
    }

    return 0;
}

AudioDeviceInfo? ResolveDevice(string value, DeviceDirection direction, IReadOnlyList<AudioDeviceInfo> candidates)
{
    if (value == "default")
    {
        return devices.GetDefaultDevice(direction);
    }

    if (int.TryParse(value, out var number) && number >= 1 && number <= candidates.Count)
    {
        return candidates[number - 1];
    }

    return candidates.FirstOrDefault(d => d.Id == value)
        ?? candidates.FirstOrDefault(d => d.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
}
