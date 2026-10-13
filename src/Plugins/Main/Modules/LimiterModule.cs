using System.ComponentModel.DataAnnotations;
using Micser.Audio;

namespace Micser.Plugins.Main.Modules;

/// <param name="Ceiling">Highest output level in dB.</param>
/// <param name="Gain">Gain in dB before limiting.</param>
/// <param name="Release">Time in seconds the gain takes to come back after a peak.</param>
public sealed record LimiterState(
    [Range(-24f, 0f)] float Ceiling = -1f,
    [Range(0f, 24f)] float Gain = 0f,
    [Range(0.001f, 2f)] float Release = 0.1f
);

/// <summary>
/// Stereo-linked brickwall limiter: no sample of any channel leaves above <see cref="Ceiling"/>. The audio is delayed by
/// <see cref="Lookahead"/>, so the gain is already down when a peak arrives.
/// </summary>
/// <remarks>
/// The gain each frame needs is held at the minimum over the lookahead window (W frames), released with <see cref="Release"/>, and smoothed
/// with a moving average over W frames. The audio is delayed by W - 1 frames, so the average for a peak's output covers only values held
/// from the peak and is at most the gain the peak needs.
/// </remarks>
public class LimiterModule : EffectModule, IStatefulModule<LimiterState>
{
    /// <summary>
    /// The lookahead in seconds, which is also the latency the limiter adds.
    /// </summary>
    public const float Lookahead = 0.005f;

    private float _alphaRelease;
    private float _appliedGain = float.NaN;
    private float[] _average = [];
    private int _averagePosition;
    private double _averageSum;
    private int _channelCountProcessed;
    private float _envelope = 1f;
    private float[] _gains = [];
    private int _linePosition;
    private float[][] _lines = [];
    private MinimumWindow _minimum = new(2);
    private float _release = float.NaN;
    private int _window;

    /// <summary>
    /// Highest output level in dB.
    /// </summary>
    public float Ceiling { get; set; } = -1f;

    /// <summary>
    /// Gain in dB before limiting.
    /// </summary>
    public float Gain { get; set; }

    /// <summary>
    /// Time in seconds the gain takes to come back after a peak.
    /// </summary>
    public float Release { get; set; } = 0.1f;

    public LimiterState GetState()
    {
        return new LimiterState(Ceiling, Gain, Release);
    }

    public void SetState(LimiterState state)
    {
        (Ceiling, Gain, Release) = (state.Ceiling, state.Gain, state.Release);
    }

    protected override void OnAttached()
    {
        _window = Math.Max(2, (int)MathF.Round(Lookahead * Format.SampleRate));
        _gains = new float[Format.FrameCount];
        _average = new float[_window];
        Array.Fill(_average, 1f);
        _averageSum = _window;
        _averagePosition = 0;
        _linePosition = 0;
        _minimum = new MinimumWindow(_window);
        _envelope = 1f;
        _lines = [];
        _channelCountProcessed = 0;
        EnsureChannels(2);
    }

    protected override void Process(AudioBuffer buffer)
    {
        var ceiling = Decibels.ToLinear(Math.Min(Ceiling, 0f));
        var inputGain = Decibels.ToLinear(Math.Max(Gain, 0f));
        UpdateRelease();

        var channelCount = buffer.ChannelCount;
        EnsureChannels(channelCount);
        // channels that weren't processed in the last block start from silence
        for (var c = _channelCountProcessed; c < channelCount; c++)
        {
            Array.Clear(_lines[c]);
        }

        _channelCountProcessed = channelCount;

        // the input gain ramps over the block, like the gain module
        var startGain = float.IsNaN(_appliedGain) ? inputGain : _appliedGain;
        _appliedGain = inputGain;
        var step = (inputGain - startGain) / _gains.Length;

        var gains = _gains.AsSpan();
        gains.Clear();
        for (var c = 0; c < channelCount; c++)
        {
            var channel = buffer.GetChannel(c);
            for (var i = 0; i < channel.Length; i++)
            {
                channel[i] *= startGain + (step * (i + 1));
                gains[i] = Math.Max(gains[i], Math.Abs(channel[i]));
            }
        }

        for (var i = 0; i < gains.Length; i++)
        {
            gains[i] = NextGain(gains[i] > ceiling ? ceiling / gains[i] : 1f);
        }

        for (var c = 0; c < channelCount; c++)
        {
            var channel = buffer.GetChannel(c);
            var line = _lines[c];
            for (var i = 0; i < channel.Length; i++)
            {
                var position = (_linePosition + i) % line.Length;
                var delayed = line[position];
                line[position] = channel[i];
                channel[i] = Math.Clamp(delayed * gains[i], -ceiling, ceiling);
            }
        }

        _linePosition = (_linePosition + gains.Length) % (_window - 1);
    }

    private void EnsureChannels(int channelCount)
    {
        if (_lines.Length >= channelCount)
        {
            return;
        }

        var lines = new float[channelCount][];
        _lines.CopyTo(lines, 0);
        for (var c = _lines.Length; c < channelCount; c++)
        {
            // window - 1 frames: the average reaches the gain a peak needs exactly window - 1 frames after the peak
            lines[c] = new float[_window - 1];
        }

        _lines = lines;
    }

    /// <summary>
    /// Takes the gain the newest frame needs and returns the gain for the frame that leaves the delay line now.
    /// </summary>
    private float NextGain(float needed)
    {
        var held = _minimum.Push(needed);
        _envelope = held < _envelope ? held : held + (_alphaRelease * (_envelope - held));

        // moving average over the window; a double keeps the running sum from drifting
        _averageSum += _envelope - _average[_averagePosition];
        _average[_averagePosition] = _envelope;
        _averagePosition = (_averagePosition + 1) % _window;
        return (float)(_averageSum / _window);
    }

    private void UpdateRelease()
    {
        var release = Release;
        if (release == _release)
        {
            return;
        }

        _release = release;
        _alphaRelease = MathF.Exp(-1f / (Format.SampleRate * Math.Max(release, 1e-5f)));
    }

    /// <summary>
    /// The minimum of the last values pushed, over a fixed window, in constant amortized time (a monotonic queue).
    /// </summary>
    private sealed class MinimumWindow
    {
        private readonly long[] _indices;
        private readonly float[] _values;
        private readonly int _window;
        private int _head;
        private long _next;
        private int _size;

        public MinimumWindow(int window)
        {
            _window = window;
            _indices = new long[window];
            _values = new float[window];
        }

        public float Push(float value)
        {
            if (_size > 0 && _indices[_head] <= _next - _window)
            {
                _head = (_head + 1) % _window;
                _size--;
            }

            while (_size > 0 && _values[(_head + _size - 1) % _window] >= value)
            {
                _size--;
            }

            var tail = (_head + _size) % _window;
            _indices[tail] = _next;
            _values[tail] = value;
            _size++;
            _next++;
            return _values[_head];
        }
    }
}
