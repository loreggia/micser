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
    private readonly Dictionary<(ChannelLayout Source, ChannelLayout Target), ChannelMixer> _mixers = [];

    internal InputPort(AudioModule module, string name, ChannelLayout? layout)
        : base(module, name)
    {
        Layout = layout;
    }

    /// <summary>
    /// The layout the connected outputs are mixed into. When null, the widest layout of the connected outputs is used.
    /// </summary>
    public ChannelLayout? Layout { get; set; }

    internal void Mix(ReadOnlySpan<OutputPort> sources)
    {
        var target = Layout ?? GetWidestLayout(sources);
        Buffer.SetLayout(target);
        Buffer.Clear();

        foreach (var source in sources)
        {
            var sourceBuffer = source.Buffer;
            if (!_mixers.TryGetValue((sourceBuffer.Layout, target), out var mixer))
            {
                mixer = new ChannelMixer(sourceBuffer.Layout, target);
                _mixers.Add((sourceBuffer.Layout, target), mixer);
            }

            mixer.MixInto(sourceBuffer, Buffer);
        }
    }

    private static ChannelLayout GetWidestLayout(ReadOnlySpan<OutputPort> sources)
    {
        var layout = ChannelLayout.None;
        foreach (var source in sources)
        {
            var candidate = source.Buffer.Layout;
            if (candidate.ChannelCount > layout.ChannelCount ||
                (candidate.ChannelCount == layout.ChannelCount && candidate.HasSpeakerPositions && !layout.HasSpeakerPositions))
            {
                layout = candidate;
            }
        }

        return layout;
    }
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
}
