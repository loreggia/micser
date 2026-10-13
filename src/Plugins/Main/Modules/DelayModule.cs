using System.ComponentModel.DataAnnotations;
using Micser.Audio;

namespace Micser.Plugins.Main.Modules;

/// <param name="Time">Extra delay of the channel in seconds, added to <see cref="DelayState.Time"/>.</param>
public sealed record DelayChannel([Range(0f, DelayModule.MaxChannelTime)] float Time);

/// <param name="Time">Delay of all channels in seconds.</param>
/// <param name="Channels">Extra delays by channel index; channels without an entry have none.</param>
public sealed record DelayState(
    [Range(0f, DelayModule.MaxTime)] float Time,
    [MaxLength(AudioModule.MaxChannelCount)] IReadOnlyList<DelayChannel> Channels
);

/// <summary>
/// Delays the audio, e.g. to align paths with different latencies. A change of a delay crossfades from the old to the new one over one
/// block. The total delay of a channel is limited to <see cref="MaxTime"/>.
/// </summary>
public class DelayModule : EffectModule, IStatefulModule<DelayState>
{
    /// <summary>
    /// The highest extra delay of a channel in seconds.
    /// </summary>
    public const float MaxChannelTime = 0.1f;

    /// <summary>
    /// The highest delay of a channel in seconds, also with its extra delay.
    /// </summary>
    public const float MaxTime = 2f;

    private int[] _appliedFrames = [];
    private int _lineLength;
    private float[][] _lines = [];
    private int _maxFrames;
    private int _processedChannels;
    private int _writePosition;

    /// <summary>
    /// Extra delays by channel index; replace the list to change them.
    /// </summary>
    public IReadOnlyList<DelayChannel> Channels { get; set; } = [];

    /// <summary>
    /// Delay of all channels in seconds.
    /// </summary>
    public float Time { get; set; }

    public DelayState GetState()
    {
        return new DelayState(Time, Channels);
    }

    public void SetState(DelayState state)
    {
        (Time, Channels) = (state.Time, [.. state.Channels]);
    }

    protected override void OnAttached()
    {
        _maxFrames = (int)(MaxTime * Format.SampleRate);
        // the block is written before it is read, so the line holds the longest delay on top of one block
        _lineLength = _maxFrames + Format.FrameCount;
        _lines = [];
        _appliedFrames = [];
        _processedChannels = 0;
        _writePosition = 0;
        EnsureChannels(2);
    }

    protected override void Process(AudioBuffer buffer)
    {
        var time = Time;
        var channels = Channels;
        var channelCount = buffer.ChannelCount;
        EnsureChannels(channelCount);

        // channels that weren't processed in the last block start from silence
        for (var c = _processedChannels; c < channelCount; c++)
        {
            Array.Clear(_lines[c]);
            _appliedFrames[c] = GetFrames(time, channels, c);
        }

        _processedChannels = channelCount;

        for (var c = 0; c < channelCount; c++)
        {
            var samples = buffer.GetChannel(c);
            var line = _lines[c];
            Write(line, samples);

            var frames = GetFrames(time, channels, c);
            var applied = _appliedFrames[c];
            if (frames == applied)
            {
                Read(line, samples, frames);
            }
            else
            {
                Crossfade(line, samples, applied, frames);
                _appliedFrames[c] = frames;
            }
        }

        _writePosition = (_writePosition + Format.FrameCount) % _lineLength;
    }

    private void Crossfade(float[] line, Span<float> samples, int fromFrames, int toFrames)
    {
        var step = 1f / samples.Length;
        for (var i = 0; i < samples.Length; i++)
        {
            var weight = (i + 1) * step;
            var from = line[Position(i - fromFrames)];
            var to = line[Position(i - toFrames)];
            samples[i] = from + ((to - from) * weight);
        }
    }

    /// <summary>
    /// Makes room for the channels. Allocates only when the channel count grows beyond all earlier ones.
    /// </summary>
    private void EnsureChannels(int channelCount)
    {
        if (_lines.Length >= channelCount)
        {
            return;
        }

        var lines = new float[channelCount][];
        var appliedFrames = new int[channelCount];
        _lines.CopyTo(lines, 0);
        _appliedFrames.CopyTo(appliedFrames, 0);
        for (var c = _lines.Length; c < channelCount; c++)
        {
            lines[c] = new float[_lineLength];
        }

        _lines = lines;
        _appliedFrames = appliedFrames;
    }

    private int GetFrames(float time, IReadOnlyList<DelayChannel> channels, int channel)
    {
        var total = time + (channel < channels.Count ? channels[channel].Time : 0f);
        return Math.Clamp((int)MathF.Round(total * Format.SampleRate), 0, _maxFrames);
    }

    private int Position(int offset)
    {
        var position = (_writePosition + offset) % _lineLength;
        return position < 0 ? position + _lineLength : position;
    }

    private void Read(float[] line, Span<float> samples, int frames)
    {
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = line[Position(i - frames)];
        }
    }

    private void Write(float[] line, ReadOnlySpan<float> samples)
    {
        for (var i = 0; i < samples.Length; i++)
        {
            line[Position(i)] = samples[i];
        }
    }
}
