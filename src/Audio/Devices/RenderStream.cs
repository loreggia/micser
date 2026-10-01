using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;

namespace Micser.Audio.Devices;

/// <summary>
/// Plays blocks at the engine sample rate on a device. <see cref="Write"/> resamples each block on the audio thread
/// into a ring buffer that the device thread drains; the resampling ratio compensates the clock drift.
/// </summary>
public sealed class RenderStream : IDeviceStream
{
    /// <summary>
    /// The shared-mode device buffer. Windows keeps it at two device periods at least; NAudio's default is 200 ms.
    /// </summary>
    private const int DeviceBufferMilliseconds = 20;

    private readonly int _channels;
    private readonly MMDevice _device;
    private readonly DriftController _drift;
    private readonly ProcessingFormat _format;
    private readonly float[] _interleaved;
    private readonly ILogger _logger;
    private readonly long _openedAt = Environment.TickCount64;
    private readonly float[] _output;
    private readonly WasapiPlayer _player;
    private readonly WdlResampler _resampler;
    private readonly SampleRingBuffer _ring;
    private readonly AdaptiveTarget _target;
    private volatile bool _isConsuming;
    private volatile bool _isStopped;
    private long _lastRequest;
    private long _overruns;

    // -1 until the device consumes, so underruns while it starts don't count as dropouts
    private long _seenUnderruns = -1;

    private long _underruns;

    internal RenderStream(MMDevice device, ProcessingFormat format, ILogger logger)
    {
        _device = device;
        _format = format;
        _logger = logger;

        WaveFormat mixFormat;
        using (var client = device.CreateAudioClient())
        {
            mixFormat = client.MixFormat;
        }

        Layout = ChannelLayout.FromWaveFormat(mixFormat);
        DeviceSampleRate = mixFormat.SampleRate;
        _channels = mixFormat.Channels;

        _target = StreamBuffering.CreateRenderTarget(device, format, DeviceSampleRate);
        _drift = new DriftController(_target.Value);
        _ring = new SampleRingBuffer(DeviceSampleRate * _channels);
        _ring.WriteSilence((int)_target.Value * _channels);
        _interleaved = new float[format.FrameCount * _channels];
        _output = new float[(int)Math.Ceiling(format.FrameCount * (double)DeviceSampleRate / format.SampleRate * 1.1 + 64) * _channels];

        _resampler = new WdlResampler();
        _resampler.SetMode(true, 0, true);
        _resampler.SetFilterParms();
        _resampler.SetFeedMode(true);

        _player = new WasapiPlayerBuilder()
            .WithDevice(device)
            .WithSharedMode()
            .WithEventSync()
            .WithLatency(DeviceBufferMilliseconds)
            .WithMmcssThreadPriority("Pro Audio")
            .Build();
        logger.LogInformation("Playback on {Device}: latency {Latency} ms, low latency {LowLatency} ({Reason}).", device.FriendlyName, _player.LatencyMilliseconds, _player.LowLatencyActive, _player.LowLatencyUnavailableReason);
        _player.PlaybackStopped += OnPlaybackStopped;
        _player.Init(new RingBufferWaveProvider(this, new WaveFormatExtensible(DeviceSampleRate, 32, _channels, true, 32, (Speakers)GetChannelMask(mixFormat))));
        _player.Play();
    }

    public int DeviceSampleRate { get; }

    public bool IsFaulted => _isStopped || StreamBuffering.IsStalled(_openedAt, Interlocked.Read(ref _lastRequest), Environment.TickCount64);

    public ChannelLayout Layout { get; }

    public StreamStatistics Statistics => new(
        _drift.SmoothedFill,
        _drift.TargetFill,
        _drift.Correction,
        Interlocked.Read(ref _underruns),
        Interlocked.Read(ref _overruns),
        _drift.TargetFill * 1000 / DeviceSampleRate);

    public void Dispose()
    {
        _player.PlaybackStopped -= OnPlaybackStopped;
        _player.Dispose();
        _device.Dispose();
    }

    /// <summary>
    /// Audio thread only. Queues a block for playback; its layout must be <see cref="Layout"/>.
    /// </summary>
    public void Write(AudioBuffer source)
    {
        if (source.Layout != Layout)
        {
            throw new ArgumentException($"Expected layout {Layout}, got {source.Layout}.", nameof(source));
        }

        if (!_isConsuming)
        {
            // the device hasn't started yet; the ring buffer holds the initial silence until it does
            return;
        }

        var underruns = Interlocked.Read(ref _underruns);
        var hadDropout = _seenUnderruns >= 0 && underruns != _seenUnderruns;
        _seenUnderruns = underruns;

        var fill = _ring.Count / _channels;
        if (_target.Update(hadDropout))
        {
            _drift.TargetFill = _target.Value;
            if (fill < _target.Value)
            {
                // a dropout already happened, so the gap to the larger target is filled right away
                fill += _ring.WriteSilence(((int)_target.Value - fill) * _channels) / _channels;
                _drift.Reset();
            }
        }

        if (fill > _drift.TargetFill * StreamBuffering.MaxFillFactor)
        {
            // the device isn't consuming, e.g. while it's being reinitialized
            Interlocked.Increment(ref _overruns);
            return;
        }

        if (fill < _drift.TargetFill * StreamBuffering.MinFillFactor)
        {
            // too far behind for the drift correction, e.g. after the device's initial buffer fill
            var missing = (int)_drift.TargetFill - fill;
            fill += _ring.WriteSilence(missing * _channels) / _channels;
            _drift.Reset();
        }

        for (var c = 0; c < _channels; c++)
        {
            var channel = source.GetChannel(c);
            for (var i = 0; i < channel.Length; i++)
            {
                _interleaved[i * _channels + c] = Math.Clamp(channel[i], -1f, 1f);
            }
        }

        var correction = _drift.Update(fill);
        _resampler.SetRates(_format.SampleRate, DeviceSampleRate / correction);

        var inputFrames = _resampler.ResamplePrepare(source.FrameCount, _channels, out var input);
        _interleaved.AsSpan(0, inputFrames * _channels).CopyTo(input);
        var produced = _resampler.ResampleOut(_output, inputFrames, _output.Length / _channels, _channels);

        if (_ring.Write(_output.AsSpan(0, produced * _channels)) < produced * _channels)
        {
            Interlocked.Increment(ref _overruns);
        }
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
        _isStopped = true;
        if (e.Exception != null)
        {
            _logger.LogWarning(e.Exception, "Playback on {Device} stopped.", _device.FriendlyName);
        }
    }

    private sealed class RingBufferWaveProvider : IWaveProvider
    {
        private readonly RenderStream _stream;

        public RingBufferWaveProvider(RenderStream stream, WaveFormat waveFormat)
        {
            _stream = stream;
            WaveFormat = waveFormat;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(Span<byte> buffer)
        {
            Interlocked.Exchange(ref _stream._lastRequest, Environment.TickCount64);
            var samples = MemoryMarshal.Cast<byte, float>(buffer);
            var read = _stream._ring.Read(samples);
            if (read < samples.Length)
            {
                samples[read..].Clear();
                Interlocked.Increment(ref _stream._underruns);
            }

            _stream._isConsuming = true;

            return buffer.Length;
        }
    }
}
