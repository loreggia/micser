using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using Micser.Audio.Devices;
using Micser.Audio.Devices.PipeWire;
using NAudio.Wave;

namespace Micser.Audio.Tests.Devices.PipeWire;

/// <summary>
/// Virtual cables (PipeWire's loopback module) against <see cref="PipeWireTestDaemon"/>; skipped where the daemon can't run.
/// </summary>
[SupportedOSPlatform("linux")]
[NotInParallel(nameof(PipeWireBackendTests))]
[ClassDataSource<PipeWireTestDaemon>(Shared = SharedType.PerTestSession)]
public class PipeWireCableTests
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private static readonly ChannelLayout Surround71 = new(
        8,
        Speakers.FrontLeft
            | Speakers.FrontRight
            | Speakers.FrontCenter
            | Speakers.LowFrequency
            | Speakers.BackLeft
            | Speakers.BackRight
            | Speakers.SideLeft
            | Speakers.SideRight
    );

    private readonly PipeWireTestDaemon _daemon;

    public PipeWireCableTests(PipeWireTestDaemon daemon)
    {
        _daemon = daemon;
    }

    [Test]
    public async Task CreatesAnInputAndAnOutputPerCable()
    {
        using var devices = CreateDevices();
        var cables = new VirtualCables(devices);

        cables.Set([ChannelLayout.Stereo, ChannelLayout.Surround51]);

        await Assert.That(cables.IsSupported).IsTrue();
        var input = devices.GetDevice("micser_cable_1_input")!;
        await Assert.That(input.Name).IsEqualTo("Micser Cable 1 Input");
        await Assert.That(input.Direction).IsEqualTo(DeviceDirection.Output);
        await Assert.That(input.Layout).IsEqualTo(ChannelLayout.Stereo);
        var output = devices.GetDevice("micser_cable_1_output")!;
        await Assert.That(output.Name).IsEqualTo("Micser Cable 1 Output");
        await Assert.That(output.Direction).IsEqualTo(DeviceDirection.Input);
        await Assert.That(devices.GetDevice("micser_cable_2_input")?.Layout).IsEqualTo(ChannelLayout.Surround51);
        await Assert.That(devices.GetDevice("micser_cable_2_output")?.Layout).IsEqualTo(ChannelLayout.Surround51);
    }

    [Test]
    public async Task PassesWhatIsPlayedToTheInputToTheOutput()
    {
        using var devices = CreateDevices();
        new VirtualCables(devices).Set([ChannelLayout.Stereo]);
        using var render = devices.OpenRender("micser_cable_1_input", ProcessingFormat.Default);
        using var capture = devices.OpenCapture("micser_cable_1_output", ProcessingFormat.Default);

        // a 1 kHz sine at -6 dBFS, whose RMS is 0.354
        var rms = Pump(render, capture, (_, frame) => 0.5f * MathF.Sin(2 * MathF.PI * 1000 * frame / 48000))
            .Select(channel => MathF.Sqrt(channel.Average(sample => sample * sample)))
            .ToArray();

        await Assert.That(rms[0]).IsBetween(0.33f, 0.37f);
        await Assert.That(rms[1]).IsBetween(0.33f, 0.37f);
    }

    [Test]
    public async Task KeepsTheChannelsOfASurroundCableApart()
    {
        using var devices = CreateDevices();
        new VirtualCables(devices).Set([Surround71]);
        using var render = devices.OpenRender("micser_cable_1_input", ProcessingFormat.Default);
        using var capture = devices.OpenCapture("micser_cable_1_output", ProcessingFormat.Default);

        var levels = Pump(render, capture, (channel, _) => 0.1f * (channel + 1)).Select(c => c.Average()).ToArray();

        for (var c = 0; c < 8; c++)
        {
            await Assert.That(levels[c]).IsBetween((0.1f * (c + 1)) - 0.01f, (0.1f * (c + 1)) + 0.01f);
        }
    }

    [Test]
    public async Task ChangesAndRemovesCables()
    {
        using var devices = CreateDevices();
        var cables = new VirtualCables(devices);
        cables.Set([ChannelLayout.Stereo, ChannelLayout.Stereo]);
        using var render = devices.OpenRender("micser_cable_1_input", ProcessingFormat.Default);

        cables.Set([Surround71]);

        await Assert.That(devices.GetDevice("micser_cable_1_input")?.Layout).IsEqualTo(Surround71);
        await Assert.That(devices.GetDevice("micser_cable_2_input")).IsNull();
        await Assert.That(devices.GetDevice("micser_cable_2_output")).IsNull();

        // the stream on the old cable 1 went with it
        await Assert.That(render.IsFaulted).IsTrue();

        cables.Set([]);
        await Assert.That(devices.GetDevice("micser_cable_1_input")).IsNull();
    }

    [Test]
    public async Task RecreatesTheCablesWhenTheDaemonRestarts()
    {
        using var devices = CreateDevices();
        new VirtualCables(devices).Set([ChannelLayout.Stereo]);
        var removed = WaitForChange(devices, "micser_cable_1_input", AudioDeviceChange.Removed);
        // the loopback's two nodes come back as separate registry globals
        var inputAdded = WaitForChange(devices, "micser_cable_1_input", AudioDeviceChange.Added);
        var outputAdded = WaitForChange(devices, "micser_cable_1_output", AudioDeviceChange.Added);

        await _daemon.RestartAsync();
        await removed.WaitAsync(EventTimeout);
        await Task.WhenAll(inputAdded, outputAdded).WaitAsync(TimeSpan.FromSeconds(15));

        await Assert.That(devices.GetDevice("micser_cable_1_input")).IsNotNull();
        await Assert.That(devices.GetDevice("micser_cable_1_output")).IsNotNull();
    }

    [Test]
    public async Task RemovesTheCablesWithTheBackend()
    {
        using (var devices = CreateDevices())
        {
            new VirtualCables(devices).Set([ChannelLayout.Stereo]);
        }

        using var other = CreateDevices();
        await Assert.That(other.GetDevice("micser_cable_1_input")).IsNull();
    }

    /// <summary>
    /// Plays a signal for 2 s in the engine's blocks and returns the captured channels of the last second, after the streams settled.
    /// </summary>
    private static float[][] Pump(RenderStream render, CaptureStream capture, Func<int, long, float> signal)
    {
        var format = ProcessingFormat.Default;
        var output = new AudioBuffer(format.FrameCount, render.Layout);
        var input = new AudioBuffer(format.FrameCount);
        var blocks = 2 * format.SampleRate / format.FrameCount;
        var captured = Enumerable.Range(0, render.Layout.ChannelCount).Select(_ => new List<float>()).ToArray();
        var period = Stopwatch.Frequency * format.FrameCount / format.SampleRate;
        var deadline = Stopwatch.GetTimestamp();

        for (var block = 0; block < blocks; block++)
        {
            for (var c = 0; c < output.ChannelCount; c++)
            {
                var channel = output.GetChannel(c);
                for (var i = 0; i < channel.Length; i++)
                {
                    channel[i] = signal(c, ((long)block * format.FrameCount) + i);
                }
            }

            render.Write(output);
            capture.Read(input);
            if (block >= blocks / 2)
            {
                for (var c = 0; c < Math.Min(input.ChannelCount, captured.Length); c++)
                {
                    captured[c].AddRange(input.GetChannel(c).ToArray());
                }
            }

            deadline += period;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                Thread.Sleep(1);
            }
        }

        return captured.Select(channel => channel.ToArray()).ToArray();
    }

    private static Task WaitForChange(AudioDeviceService devices, string deviceId, AudioDeviceChange change)
    {
        var completion = new TaskCompletionSource();
        devices.DeviceChanged += (_, e) =>
        {
            if (e.DeviceId == deviceId && e.Change == change)
            {
                completion.TrySetResult();
            }
        };
        return completion.Task;
    }

    private AudioDeviceService CreateDevices()
    {
        Skip.When(_daemon.Unavailable != null, $"No test PipeWire: {_daemon.Unavailable}.");
        var backend =
            PipeWireBackend.TryCreate(NullLogger.Instance, _daemon.Socket)
            ?? throw new InvalidOperationException("Could not connect to the test PipeWire.");
        return new AudioDeviceService(backend);
    }
}
