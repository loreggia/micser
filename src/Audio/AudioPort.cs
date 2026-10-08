namespace Micser.Audio;

public abstract class AudioPort
{
    private AudioBuffer? _buffer;

    private protected AudioPort(AudioModule module, string name)
    {
        Module = module;
        Name = name;
    }

    /// <summary>
    /// The port's buffer for the current block. Available once the module is added to a graph.
    /// </summary>
    public AudioBuffer Buffer => _buffer ?? throw new InvalidOperationException("The module has not been added to a graph.");

    public AudioModule Module { get; }

    public string Name { get; }

    public override string ToString()
    {
        return $"{Module.GetType().Name}.{Name}";
    }

    internal void Allocate(int frameCount)
    {
        _buffer = new AudioBuffer(frameCount);
    }
}

/// <summary>
/// Receives the mix of all connected outputs.
/// </summary>
public sealed class InputPort : AudioPort
{
    private readonly Dictionary<MixerKey, ChannelMixer> _mixers = [];

    internal InputPort(AudioModule module, string name, ChannelLayout? layout)
        : base(module, name)
    {
        Layout = layout;
    }

    /// <summary>
    /// The layout the connected outputs are mixed into. When null, it is <see cref="AudioModule.ChannelCount"/>'s layout or, if that isn't
    /// set either, the widest layout of the outputs connected as a whole, widened to the highest target channel of the connections to a
    /// single channel (at least stereo without outputs connected as a whole).
    /// </summary>
    public ChannelLayout? Layout { get; set; }

    internal void Mix(ReadOnlySpan<Connection> sources)
    {
        var target = Layout ?? GetLayout(sources);
        Buffer.SetLayout(target);
        Buffer.Clear();

        foreach (var source in sources)
        {
            var sourceBuffer = source.Source.Buffer;
            if (source.SourceChannel >= sourceBuffer.ChannelCount || source.TargetChannel >= target.ChannelCount)
            {
                continue;
            }

            var key = new MixerKey(sourceBuffer.Layout, target, source.SourceChannel, source.TargetChannel);
            if (!_mixers.TryGetValue(key, out var mixer))
            {
                mixer = new ChannelMixer(sourceBuffer.Layout, target, source.SourceChannel, source.TargetChannel);
                _mixers.Add(key, mixer);
            }

            mixer.MixInto(sourceBuffer, Buffer);
        }
    }

    private ChannelLayout GetLayout(ReadOnlySpan<Connection> sources)
    {
        if (Module.ChannelCount is { } channelCount)
        {
            return ChannelLayout.FromChannelCount(channelCount);
        }

        var layout = ChannelLayout.None;
        var hasWholeSources = false;
        var channelCountNeeded = 0;
        foreach (var source in sources)
        {
            if (source.TargetChannel is { } channel)
            {
                channelCountNeeded = Math.Max(channelCountNeeded, channel + 1);
                continue;
            }

            hasWholeSources = true;
            var candidate = source.Source.Buffer.Layout;
            if (candidate.ChannelCount > layout.ChannelCount ||
                (candidate.ChannelCount == layout.ChannelCount && candidate.HasSpeakerPositions && !layout.HasSpeakerPositions))
            {
                layout = candidate;
            }
        }

        if (channelCountNeeded > 0 && !hasWholeSources)
        {
            channelCountNeeded = Math.Max(channelCountNeeded, 2);
        }

        return layout.ChannelCount < channelCountNeeded ? ChannelLayout.FromChannelCount(channelCountNeeded) : layout;
    }

    private readonly record struct MixerKey(ChannelLayout Source, ChannelLayout Target, int? SourceChannel, int? TargetChannel);
}

/// <summary>
/// Holds the samples a module produces for the current block. The module sets the buffer's layout.
/// </summary>
public sealed class OutputPort : AudioPort
{
    internal OutputPort(AudioModule module, string name)
        : base(module, name)
    {
    }

    /// <summary>
    /// Measures the output after volume and mute. Available once the module is added to a graph.
    /// </summary>
    public LevelMeter? Meter { get; internal set; }
}
