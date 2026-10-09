using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;

namespace Micser.Audio.Devices;

/// <summary>
/// Captures a device (or the loopback of a render device) and delivers it in blocks at the engine sample rate.
/// The device thread fills a ring buffer; <see cref="Read"/> drains it on the audio thread through a resampler that
/// compensates the clock drift between device and engine.
/// </summary>
public sealed class CaptureStream : IDeviceStream
{
    private readonly int _channels;
    private readonly float[] _convertBuffer;
    private readonly SampleConverter _converter;

    // loopback capture gets no data while nothing plays, so it can't detect stalls
    private readonly bool _detectsStalls;

    private readonly MMDevice _device;
    private readonly DriftController _drift;
    private readonly ProcessingFormat _format;
    private readonly float[] _interleaved;
    private readonly ILogger _logger;
    private readonly long _openedAt = Environment.TickCount64;
    private readonly WasapiRecorder _recorder;
    private readonly WdlResampler _resampler;
    private readonly SampleRingBuffer _ring;
    private readonly AdaptiveTarget _target;
    private bool _hadDropout;
    private bool _isPrefilled;
    private volatile bool _isStopped;
    private long _lastData;
    private long _lastRead;
    private long _overruns;
    private long _resyncs;
    private long _underruns;

    internal CaptureStream(
        MMDevice device,
        bool loopback,
        ProcessingFormat format,
        double? initialTargetMilliseconds,
        ILogger logger
    )
    {
        _device = device;
        _format = format;
        _logger = logger;
        _detectsStalls = !loopback;

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
            device.FriendlyName,
            _recorder.LatencyMilliseconds,
            _recorder.LowLatencyActive,
            _recorder.LowLatencyUnavailableReason
        );

        var deviceFormat = _recorder.WaveFormat;
        Layout = ChannelLayout.FromWaveFormat(deviceFormat);
        DeviceSampleRate = deviceFormat.SampleRate;
        _channels = deviceFormat.Channels;
        _converter = new SampleConverter(deviceFormat);
        _convertBuffer = new float[deviceFormat.SampleRate * _channels];
        _ring = new SampleRingBuffer(deviceFormat.SampleRate * _channels);
        _interleaved = new float[format.FrameCount * _channels];
        _target = StreamBuffering.CreateCaptureTarget(device, format, DeviceSampleRate, initialTargetMilliseconds);
        _drift = new DriftController(_target.Value);

        _resampler = new WdlResampler();
        _resampler.SetMode(true, 0, true);
        _resampler.SetFilterParms();
        _resampler.SetFeedMode(false);

        _recorder.DataAvailable += OnDataAvailable;
        _recorder.RecordingStopped += OnRecordingStopped;
        _recorder.StartRecording();
    }

    public int DeviceSampleRate { get; }

    public bool IsFaulted =>
        _isStopped
        || (
            _detectsStalls
            && StreamBuffering.IsStalled(_openedAt, Interlocked.Read(ref _lastData), Environment.TickCount64)
        );

    public ChannelLayout Layout { get; }

    public StreamStatistics Statistics =>
        new(
            _drift.SmoothedFill,
            _drift.TargetFill,
            _drift.Correction,
            Interlocked.Read(ref _underruns),
            Interlocked.Read(ref _overruns),
            _drift.TargetFill * 1000 / DeviceSampleRate,
            Interlocked.Read(ref _resyncs)
        );

    public void Dispose()
    {
        _recorder.DataAvailable -= OnDataAvailable;
        _recorder.RecordingStopped -= OnRecordingStopped;
        _recorder.Dispose();
        _device.Dispose();
    }

    /// <summary>
    /// Audio thread only. Fills <paramref name="destination"/> with the next block; silence until enough data is buffered.
    /// </summary>
    public void Read(AudioBuffer destination)
    {
        var now = Environment.TickCount64;
        var wasIdle = StreamBuffering.IsIdle(Interlocked.Exchange(ref _lastRead, now), now);
        destination.SetLayout(Layout);

        // after an underrun the stream refills to the new target before reading again
        if (_target.Update(_hadDropout))
        {
            _drift.TargetFill = _target.Value;
        }

        _hadDropout = false;

        var fill = _ring.Count / _channels;
        if (!_isPrefilled)
        {
            if (fill < _drift.TargetFill)
            {
                destination.Clear();
                return;
            }

            _isPrefilled = true;
            _drift.Reset();
        }

        if (fill > _drift.TargetFill * StreamBuffering.MaxFillFactor)
        {
            // too far behind for the drift correction, e.g. after the engine was paused
            var excess = fill - (int)_drift.TargetFill;
            _ring.Discard(excess * _channels);
            fill -= excess;
            _drift.Reset();
            if (!wasIdle && !StreamBuffering.IsSettling(_openedAt, now))
            {
                Interlocked.Increment(ref _resyncs);
                _hadDropout = true;
            }
        }

        var correction = _drift.Update(fill);
        _resampler.SetRates(DeviceSampleRate * correction, _format.SampleRate);

        var inputFrames = _resampler.ResamplePrepare(destination.FrameCount, _channels, out var input);
        var inputSamples = inputFrames * _channels;
        var read = _ring.Read(input[..inputSamples]);
        if (read < inputSamples)
        {
            input[read..inputSamples].Clear();
            _isPrefilled = false;
            if (!StreamBuffering.IsSettling(_openedAt, now))
            {
                Interlocked.Increment(ref _underruns);
                _hadDropout = true;
            }
        }

        var produced = _resampler.ResampleOut(_interleaved, inputFrames, destination.FrameCount, _channels);
        _interleaved.AsSpan(produced * _channels).Clear();

        for (var c = 0; c < _channels; c++)
        {
            var channel = destination.GetChannel(c);
            for (var i = 0; i < channel.Length; i++)
            {
                channel[i] = _interleaved[i * _channels + c];
            }
        }
    }

    private void OnDataAvailable(
        ReadOnlySpan<byte> data,
        AudioClientBufferFlags flags,
        long devicePosition,
        long qpcPosition
    )
    {
        Interlocked.Exchange(ref _lastData, Environment.TickCount64);
        var samples = data.Length / _converter.BytesPerSample;
        int written;

        if ((flags & AudioClientBufferFlags.Silent) != 0)
        {
            written = _ring.WriteSilence(samples);
        }
        else
        {
            var converted = _converter.Convert(data, _convertBuffer);
            written = _ring.Write(_convertBuffer.AsSpan(0, converted));
        }

        // while the engine doesn't read (e.g. audio switched off) the ring buffer fills up; the reader discards the excess when it resumes
        if (written < samples && !StreamBuffering.IsIdle(Interlocked.Read(ref _lastRead), Environment.TickCount64))
        {
            Interlocked.Increment(ref _overruns);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _isStopped = true;
        if (e.Exception != null)
        {
            _logger.LogWarning(e.Exception, "Capture from {Device} stopped.", _device.FriendlyName);
        }
    }
}
