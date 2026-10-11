using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Micser.Audio.Devices.Wasapi;

/// <summary>
/// Shared-mode, event-driven WASAPI capture of an input device or the loopback of an output device.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WasapiCaptureDevice : ICaptureDevice
{
    private readonly float[] _convertBuffer;
    private readonly SampleConverter _converter;
    private readonly MMDevice _device;
    private readonly WasapiRecorder _recorder;
    private ICaptureHandler? _handler;

    public WasapiCaptureDevice(MMDevice device, bool loopback, ILogger logger)
    {
        _device = device;
        Name = device.FriendlyName;
        PeriodSeconds = WasapiDevices.GetDevicePeriod(device);

        var builder = new WasapiRecorderBuilder()
            .WithDevice(device)
            .WithSharedMode()
            .WithEventSync()
            .WithMmcssThreadPriority("Pro Audio");
        if (loopback)
        {
            builder = builder.WithLoopbackCapture();
        }

        _recorder = builder.Build();
        logger.LogInformation(
            "Capture from {Device}: latency {Latency} ms, low latency {LowLatency} ({Reason}).",
            Name,
            _recorder.LatencyMilliseconds,
            _recorder.LowLatencyActive,
            _recorder.LowLatencyUnavailableReason
        );

        var deviceFormat = _recorder.WaveFormat;
        Layout = ChannelLayout.FromWaveFormat(deviceFormat);
        SampleRate = deviceFormat.SampleRate;
        _converter = new SampleConverter(deviceFormat);
        _convertBuffer = new float[deviceFormat.SampleRate * deviceFormat.Channels];
    }

    public ChannelLayout Layout { get; }

    public string Name { get; }

    public double PeriodSeconds { get; }

    public int SampleRate { get; }

    public void Dispose()
    {
        _recorder.DataAvailable -= OnDataAvailable;
        _recorder.RecordingStopped -= OnRecordingStopped;
        _recorder.Dispose();
        _device.Dispose();
    }

    public void Start(ICaptureHandler handler)
    {
        _handler = handler;
        _recorder.DataAvailable += OnDataAvailable;
        _recorder.RecordingStopped += OnRecordingStopped;
        _recorder.StartRecording();
    }

    private void OnDataAvailable(
        ReadOnlySpan<byte> data,
        AudioClientBufferFlags flags,
        long devicePosition,
        long qpcPosition
    )
    {
        if ((flags & AudioClientBufferFlags.Silent) != 0)
        {
            _handler!.OnSilence(data.Length / _converter.BytesPerSample);
        }
        else
        {
            var converted = _converter.Convert(data, _convertBuffer);
            _handler!.OnData(_convertBuffer.AsSpan(0, converted));
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _handler!.OnStopped(e.Exception);
    }
}
