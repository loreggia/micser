using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Micser.Audio.Devices.Wasapi;

/// <summary>
/// Shared-mode, event-driven WASAPI playback in the device's mix format, as 32-bit float.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WasapiRenderDevice : IRenderDevice
{
    /// <summary>
    /// The shared-mode device buffer. Windows keeps it at two device periods at least; NAudio's default is 200 ms.
    /// </summary>
    private const int DeviceBufferMilliseconds = 20;

    private readonly MMDevice _device;
    private readonly int _channelMask;
    private readonly WasapiPlayer _player;
    private IRenderHandler? _handler;

    public WasapiRenderDevice(MMDevice device, ILogger logger)
    {
        _device = device;
        Name = device.FriendlyName;
        PeriodSeconds = WasapiDevices.GetDevicePeriod(device);

        WaveFormat mixFormat;
        using (var client = device.CreateAudioClient())
        {
            mixFormat = client.MixFormat;
        }

        Layout = ChannelLayout.FromWaveFormat(mixFormat);
        SampleRate = mixFormat.SampleRate;
        _channelMask = GetChannelMask(mixFormat);

        _player = new WasapiPlayerBuilder()
            .WithDevice(device)
            .WithSharedMode()
            .WithEventSync()
            .WithLatency(DeviceBufferMilliseconds)
            .WithMmcssThreadPriority("Pro Audio")
            .Build();
        logger.LogInformation(
            "Playback on {Device}: latency {Latency} ms, low latency {LowLatency} ({Reason}).",
            Name,
            _player.LatencyMilliseconds,
            _player.LowLatencyActive,
            _player.LowLatencyUnavailableReason
        );
    }

    public ChannelLayout Layout { get; }

    public string Name { get; }

    public double PeriodSeconds { get; }

    public int SampleRate { get; }

    public void Dispose()
    {
        _player.PlaybackStopped -= OnPlaybackStopped;
        _player.Dispose();
        _device.Dispose();
    }

    public void Start(IRenderHandler handler)
    {
        _handler = handler;
        _player.PlaybackStopped += OnPlaybackStopped;
        _player.Init(
            new HandlerWaveProvider(
                handler,
                new WaveFormatExtensible(SampleRate, 32, Layout.ChannelCount, true, 32, (Speakers)_channelMask)
            )
        );
        _player.Play();
    }

    private static int GetChannelMask(WaveFormat format)
    {
        if (format is WaveFormatExtensible extensible && extensible.ChannelMask != 0)
        {
            return extensible.ChannelMask;
        }

        return (int)ChannelLayout.FromChannelCount(format.Channels).Speakers;
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        _handler!.OnStopped(e.Exception);
    }

    private sealed class HandlerWaveProvider : IWaveProvider
    {
        private readonly IRenderHandler _handler;

        public HandlerWaveProvider(IRenderHandler handler, WaveFormat waveFormat)
        {
            _handler = handler;
            WaveFormat = waveFormat;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(Span<byte> buffer)
        {
            _handler.OnRead(MemoryMarshal.Cast<byte, float>(buffer));
            return buffer.Length;
        }
    }
}
