namespace Micser.Audio;

/// <summary>
/// A node of the audio graph. Parameters may be changed from any thread; <see cref="Process"/> runs on the audio thread.
/// </summary>
public abstract class AudioModule : IDisposable
{
    /// <summary>
    /// The highest <see cref="ChannelCount"/>.
    /// </summary>
    public const int MaxChannelCount = 64;

    private readonly List<InputPort> _inputs = [];
    private readonly List<OutputPort> _outputs = [];
    private float _appliedVolume = 1f;
    private volatile int _channelCount;
    private ProcessingFormat? _format;
    private LevelMeter? _meter;
    private float _volume = 1f;

    /// <summary>
    /// Raised when the module changes its own state, e.g. a device module that switched to another device.
    /// Not raised for changes made through <see cref="IStatefulModule{TState}.SetState"/>.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// The channel count of the inputs without a fixed layout (<see cref="InputPort.Layout"/> null), 1..64; null takes it from their sources.
    /// </summary>
    public int? ChannelCount
    {
        get => _channelCount == 0 ? null : _channelCount;
        set
        {
            if (value is { } count)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxChannelCount);
            }

            _channelCount = value ?? 0;
        }
    }

    /// <summary>
    /// Available once the module is added to a graph.
    /// </summary>
    public ProcessingFormat Format => _format ?? throw new InvalidOperationException("The module has not been added to a graph.");

    public IReadOnlyList<InputPort> Inputs => _inputs;

    public bool IsAttached => _format != null;

    public bool IsMuted { get; set; }

    public IReadOnlyList<OutputPort> Outputs => _outputs;

    /// <summary>
    /// Output volume (0..1), applied to all outputs with a ramp over one block.
    /// </summary>
    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public InputPort GetInput(string name)
    {
        return _inputs.Find(p => p.Name == name) ?? throw new ArgumentException($"{GetType().Name} has no input '{name}'.", nameof(name));
    }

    public OutputPort GetOutput(string name)
    {
        return _outputs.Find(p => p.Name == name) ?? throw new ArgumentException($"{GetType().Name} has no output '{name}'.", nameof(name));
    }

    /// <summary>
    /// Reads the levels of what the module passes on, after volume and mute: of each output, or for a module without outputs of the signal
    /// given to <see cref="ApplyVolume"/> (e.g. what a device output plays). Empty until the module is added to a graph. Call from one
    /// thread only; reading resets the peaks.
    /// </summary>
    public PortLevels[] ReadLevels()
    {
        if (_meter != null)
        {
            return [new PortLevels(null, _meter.Read())];
        }

        var levels = new List<PortLevels>(_outputs.Count);
        foreach (var output in _outputs)
        {
            if (output.Meter is { } meter)
            {
                levels.Add(new PortLevels(output.Name, meter.Read()));
            }
        }

        return [.. levels];
    }

    internal void Attach(ProcessingFormat format)
    {
        if (_format != null)
        {
            throw new InvalidOperationException("The module is already part of a graph.");
        }

        _format = format;
        foreach (var port in _inputs)
        {
            port.Allocate(format.FrameCount);
        }

        foreach (var port in _outputs)
        {
            port.Allocate(format.FrameCount);
            port.Meter = new LevelMeter(format);
        }

        _meter = _outputs.Count == 0 ? new LevelMeter(format) : null;
        OnAttached();
    }

    internal void Detach()
    {
        _format = null;
    }

    internal void ProcessBlock()
    {
        Process();

        if (_outputs.Count > 0)
        {
            var (start, end) = NextVolumeRamp();
            foreach (var output in _outputs)
            {
                output.Buffer.ApplyGain(start, end);
                output.Meter?.Measure(output.Buffer);
                output.PublishLayout();
            }
        }
    }

    protected InputPort AddInput(string name, ChannelLayout? layout = null)
    {
        var port = new InputPort(this, name, layout);
        _inputs.Add(port);
        return port;
    }

    protected OutputPort AddOutput(string name)
    {
        var port = new OutputPort(this, name);
        _outputs.Add(port);
        return port;
    }

    /// <summary>
    /// Applies <see cref="Volume"/> and <see cref="IsMuted"/> to a buffer and measures its level. Only needed by modules without outputs;
    /// for all others the graph does this for the outputs.
    /// </summary>
    protected void ApplyVolume(AudioBuffer buffer)
    {
        var (start, end) = NextVolumeRamp();
        buffer.ApplyGain(start, end);
        _meter?.Measure(buffer);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// Called when the module is added to a graph; <see cref="Format"/> is available from here on.
    /// </summary>
    protected virtual void OnAttached()
    {
    }

    protected void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Processes one block: reads the input buffers (already mixed) and writes the output buffers, including their layout.
    /// </summary>
    protected abstract void Process();

    private (float Start, float End) NextVolumeRamp()
    {
        var start = _appliedVolume;
        var end = IsMuted ? 0f : _volume;
        _appliedVolume = end;
        return (start, end);
    }
}
