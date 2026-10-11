using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using Micser.Audio.Devices;
using Micser.Audio.Devices.PipeWire;

namespace Micser.Audio.Tests.Devices.PipeWire;

/// <summary>
/// The PipeWire backend against <see cref="PipeWireTestDaemon"/>'s virtual devices; skipped where the daemon can't run.
/// </summary>
[SupportedOSPlatform("linux")]
[NotInParallel(nameof(PipeWireBackendTests))]
[ClassDataSource<PipeWireTestDaemon>(Shared = SharedType.PerTestSession)]
public class PipeWireBackendTests
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private readonly PipeWireTestDaemon _daemon;

    public PipeWireBackendTests(PipeWireTestDaemon daemon)
    {
        _daemon = daemon;
    }

    [Test]
    public async Task ListsTheSinksAndSources()
    {
        using var devices = CreateDevices();

        var outputs = devices.GetDevices(DeviceDirection.Output);
        var inputs = devices.GetDevices(DeviceDirection.Input);

        await Assert.That(outputs.Select(d => d.Id)).Contains(PipeWireTestDaemon.SinkName);
        await Assert.That(outputs.Select(d => d.Id)).Contains(PipeWireTestDaemon.SurroundName);
        var sink = devices.GetDevice(PipeWireTestDaemon.SinkName)!;
        await Assert.That(sink.Name).IsEqualTo("Test Sink");
        await Assert.That(sink.Layout).IsEqualTo(ChannelLayout.Stereo);
        await Assert.That(sink.SampleRate).IsEqualTo(48000);
        await Assert.That(sink.IsActive).IsTrue();
        await Assert.That(devices.GetDevice(PipeWireTestDaemon.SurroundName)!.Layout?.ChannelCount).IsEqualTo(6);
        var source = inputs.Single(d => d.Id == PipeWireTestDaemon.SourceName);
        await Assert.That(source.Layout).IsEqualTo(ChannelLayout.Mono);
        await Assert.That(devices.GetDefaultDevice(DeviceDirection.Output)?.Id).IsEqualTo(PipeWireTestDaemon.SinkName);
    }

    [Test]
    public async Task KeepsTheDefaultsWhenAnObjectGoesAway()
    {
        using var devices = CreateDevices();
        using var volume = new SystemVolume(devices);
        var level = volume.Level;
        var removed = WaitForChange(devices, "micser-test-transient", AudioDeviceChange.Removed);

        // when an object with properties in the metadata goes away (e.g. a desktop's volume feedback sound), they're removed in one event
        await _daemon.PwCliAsync(
            "create-node",
            "adapter",
            "{ factory.name = support.null-audio-sink node.name = micser-test-transient media.class = Audio/Sink "
                + "audio.position = [ FL FR ] object.linger = true }"
        );
        var id = (await WaitForNodeIdAsync("micser-test-transient")).ToString();
        await _daemon.PwMetadataAsync(id, "target.object", PipeWireTestDaemon.SinkName);
        await _daemon.PwCliAsync("destroy", id);
        await removed.WaitAsync(EventTimeout);

        await Assert.That(devices.GetDefaultDevice(DeviceDirection.Output)?.Id).IsEqualTo(PipeWireTestDaemon.SinkName);
        await Assert.That(volume.Level).IsNotNull().And.IsEqualTo(level);
    }

    [Test]
    public async Task OpenStream_RejectsUnknownDevicesAndWrongDirections()
    {
        using var devices = CreateDevices();

        await Assert
            .That(() => devices.OpenRender("no-such-device", ProcessingFormat.Default))
            .Throws<InvalidOperationException>();
        await Assert
            .That(() => devices.OpenCapture(PipeWireTestDaemon.SinkName, ProcessingFormat.Default))
            .Throws<InvalidOperationException>();
        await Assert
            .That(() => devices.OpenRender(PipeWireTestDaemon.SourceName, ProcessingFormat.Default))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task PlaysToASinkAndCapturesItsMonitor()
    {
        using var devices = CreateDevices();
        using var render = devices.OpenRender(PipeWireTestDaemon.SinkName, ProcessingFormat.Default);
        using var loopback = devices.OpenLoopback(PipeWireTestDaemon.SinkName, ProcessingFormat.Default);

        // a 1 kHz sine at -6 dBFS, whose RMS is 0.354
        var rms = Pump(render, loopback, (channel, frame) => 0.5f * MathF.Sin(2 * MathF.PI * 1000 * frame / 48000))
            .Select(channel => MathF.Sqrt(channel.Average(sample => sample * sample)))
            .ToArray();

        await Assert.That(rms[0]).IsBetween(0.33f, 0.37f);
        await Assert.That(rms[1]).IsBetween(0.33f, 0.37f);
        await Assert.That(render.Statistics.Underruns).IsEqualTo(0);
        await Assert.That(loopback.Statistics.Underruns).IsEqualTo(0);
    }

    [Test]
    public async Task KeepsTheChannelsOfASurroundDeviceApart()
    {
        using var devices = CreateDevices();
        using var render = devices.OpenRender(PipeWireTestDaemon.SurroundName, ProcessingFormat.Default);
        using var loopback = devices.OpenLoopback(PipeWireTestDaemon.SurroundName, ProcessingFormat.Default);

        // a different level per channel, in the layout's order (FL FR FC LFE BL BR); the device has ALSA's (FL FR RL RR FC LFE)
        var levels = Pump(render, loopback, (channel, _) => 0.1f * (channel + 1))
            .Select(channel => channel.Average())
            .ToArray();

        for (var c = 0; c < 6; c++)
        {
            await Assert.That(levels[c]).IsBetween((0.1f * (c + 1)) - 0.01f, (0.1f * (c + 1)) + 0.01f);
        }
    }

    [Test]
    public async Task ReportsAddedAndRemovedDevices_AndFaultsTheirStreams()
    {
        using var devices = CreateDevices();
        var added = WaitForChange(devices, "micser-test-hotplug", AudioDeviceChange.Added);
        var removed = WaitForChange(devices, "micser-test-hotplug", AudioDeviceChange.Removed);

        await _daemon.PwCliAsync(
            "create-node",
            "adapter",
            "{ factory.name = support.null-audio-sink node.name = micser-test-hotplug node.description = Hotplug "
                + "media.class = Audio/Sink audio.position = [ FL FR ] object.linger = true }"
        );
        await added.WaitAsync(EventTimeout);
        await Assert.That(devices.GetDevice("micser-test-hotplug")?.Layout).IsEqualTo(ChannelLayout.Stereo);
        using var render = devices.OpenRender("micser-test-hotplug", ProcessingFormat.Default);

        await _daemon.PwCliAsync("destroy", (await _daemon.FindNodeIdAsync("micser-test-hotplug"))!.Value.ToString());
        await removed.WaitAsync(EventTimeout);

        await Assert.That(devices.GetDevice("micser-test-hotplug")).IsNull();
        await Assert.That(render.IsFaulted).IsTrue();
    }

    [Test]
    public async Task StreamsCanBeDisposedAfterTheirBackend()
    {
        var devices = CreateDevices();
        var render = devices.OpenRender(PipeWireTestDaemon.SinkName, ProcessingFormat.Default);

        devices.Dispose();
        render.Dispose();

        await Assert.That(render.IsFaulted).IsFalse();
    }

    [Test]
    public async Task ReconnectsWhenTheDaemonRestarts()
    {
        using var devices = CreateDevices();
        using var render = devices.OpenRender(PipeWireTestDaemon.SinkName, ProcessingFormat.Default);
        var removed = WaitForChange(devices, PipeWireTestDaemon.SinkName, AudioDeviceChange.Removed);
        var added = WaitForChange(devices, PipeWireTestDaemon.SinkName, AudioDeviceChange.Added);

        await _daemon.RestartAsync();
        await removed.WaitAsync(EventTimeout);
        await added.WaitAsync(TimeSpan.FromSeconds(15));

        await Assert.That(render.IsFaulted).IsTrue();
        using var newRender = devices.OpenRender(PipeWireTestDaemon.SinkName, ProcessingFormat.Default);
        using var loopback = devices.OpenLoopback(PipeWireTestDaemon.SinkName, ProcessingFormat.Default);
        var rms = Pump(newRender, loopback, (_, frame) => 0.5f * MathF.Sin(2 * MathF.PI * 1000 * frame / 48000))
            .Select(channel => MathF.Sqrt(channel.Average(sample => sample * sample)))
            .ToArray();
        await Assert.That(rms[0]).IsBetween(0.33f, 0.37f);
    }

    [Test]
    public async Task SystemVolume_FollowsTheDefaultSink()
    {
        using var devices = CreateDevices();
        using var volume = new SystemVolume(devices);
        var sinkId = (await _daemon.FindNodeIdAsync(PipeWireTestDaemon.SinkName))!.Value.ToString();

        try
        {
            // repeated changes, as from the volume keys
            for (var step = 1; step <= 5; step++)
            {
                var gain = 0.1f * step;
                var changed = WaitForVolume(volume, new SystemVolumeLevel(gain, step % 2 == 0));
                await _daemon.PwCliAsync(
                    "set-param",
                    sinkId,
                    "Props",
                    $"{{ channelVolumes = [ {gain.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 0.05 ] mute = {(step % 2 == 0 ? "true" : "false")} }}"
                );
                await changed.WaitAsync(EventTimeout);
            }
        }
        finally
        {
            await _daemon.PwCliAsync("set-param", sinkId, "Props", "{ channelVolumes = [ 1.0, 1.0 ] mute = false }");
        }
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

    private static Task WaitForVolume(SystemVolume volume, SystemVolumeLevel level)
    {
        var completion = new TaskCompletionSource();
        volume.Changed += (_, _) =>
        {
            if (volume.Level == level)
            {
                completion.TrySetResult();
            }
        };
        return completion.Task;
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

    private async Task<uint> WaitForNodeIdAsync(string nodeName)
    {
        for (var i = 0; i < 50; i++)
        {
            if (await _daemon.FindNodeIdAsync(nodeName) is { } id)
            {
                return id;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Node {nodeName} didn't appear.");
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
