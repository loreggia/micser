using System.Collections.Concurrent;
using Micser.Audio;

namespace Micser.AudioHarness;

/// <summary>
/// Emits a quiet pseudo-random noise sequence at a fixed interval and remembers the engine sample position of each.
/// </summary>
internal sealed class NoiseBurstSource : AudioModule
{
    public const float Amplitude = 0.01f;
    public const int Length = 2048;

    private readonly int _interval;
    private long _position;

    public NoiseBurstSource(int intervalSamples)
    {
        _interval = intervalSamples;
        var random = new Random(4711);
        Sequence = Enumerable.Range(0, Length).Select(_ => random.Next(2) == 0 ? -1f : 1f).ToArray();
        Output = AddOutput("Output");
    }

    public long LastBurstPosition { get; private set; } = -1;

    public OutputPort Output { get; }

    /// <summary>
    /// The emitted sequence (±1, scaled by <see cref="Amplitude"/> on output).
    /// </summary>
    public float[] Sequence { get; }

    protected override void Process()
    {
        Output.Buffer.SetLayout(ChannelLayout.Mono);
        var samples = Output.Buffer.GetChannel(0);

        for (var i = 0; i < samples.Length; i++)
        {
            var offset = (_position + i) % _interval;
            if (offset == 0)
            {
                LastBurstPosition = _position + i;
            }

            samples[i] = offset < Length ? Sequence[offset] * Amplitude : 0f;
        }

        _position += samples.Length;
    }
}

/// <summary>
/// Records its input after each burst and finds the burst's delay by cross-correlation, which works regardless of the device volume and with other
/// audio playing.
/// </summary>
internal sealed class NoiseBurstDetector : AudioModule
{
    private readonly int _maxLag;
    private readonly ConcurrentQueue<float[]> _recordings = new();
    private readonly NoiseBurstSource _source;
    private long _burst = -1;
    private long _position;
    private float[]? _recording;

    public NoiseBurstDetector(NoiseBurstSource source, int maxLagSamples)
    {
        _source = source;
        _maxLag = maxLagSamples;
        Input = AddInput("Input");
    }

    public InputPort Input { get; }

    /// <summary>
    /// Analyzes the bursts recorded since the last call and returns their delays in samples; null where no burst was found.
    /// </summary>
    public long?[] TakeLatencies()
    {
        var result = new List<long?>();
        while (_recordings.TryDequeue(out var recording))
        {
            result.Add(FindDelay(recording, _source.Sequence));
        }

        return [.. result];
    }

    protected override void Process()
    {
        var buffer = Input.Buffer;

        if (_source.LastBurstPosition != _burst)
        {
            _burst = _source.LastBurstPosition;
            _recording = new float[_maxLag + NoiseBurstSource.Length];
        }

        if (_recording != null)
        {
            for (var i = 0; i < buffer.FrameCount; i++)
            {
                var index = _position + i - _burst;
                if (index < 0 || index >= _recording.Length)
                {
                    continue;
                }

                var sum = 0f;
                for (var c = 0; c < buffer.ChannelCount; c++)
                {
                    sum += buffer.GetChannel(c)[i];
                }

                _recording[index] = sum;
            }

            if (_position + buffer.FrameCount - _burst >= _recording.Length)
            {
                _recordings.Enqueue(_recording);
                _recording = null;
            }
        }

        _position += buffer.FrameCount;
    }

    private static long? FindDelay(float[] recording, float[] sequence)
    {
        var lags = recording.Length - sequence.Length;
        var correlations = new double[lags];
        for (var lag = 0; lag < lags; lag++)
        {
            var sum = 0d;
            for (var i = 0; i < sequence.Length; i++)
            {
                sum += recording[lag + i] * sequence[i];
            }

            correlations[lag] = Math.Abs(sum);
        }

        var best = Array.IndexOf(correlations, correlations.Max());
        var mean = correlations.Average();
        return correlations[best] > 8 * mean ? best : null;
    }
}
