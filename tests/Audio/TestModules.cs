namespace Micser.Audio.Tests;

/// <summary>
/// Outputs <c>value + channel index</c> on every channel of the given layout.
/// </summary>
internal sealed class ConstantSource : AudioModule
{
    public ConstantSource(ChannelLayout layout, float value = 1f)
    {
        Layout = layout;
        Value = value;
        Output = AddOutput("Output");
    }

    public ChannelLayout Layout { get; set; }

    public Action<AudioModule>? OnProcess { get; set; }

    public OutputPort Output { get; }

    public float Value { get; set; }

    protected override void Process()
    {
        OnProcess?.Invoke(this);
        Output.Buffer.SetLayout(Layout);
        for (var c = 0; c < Layout.ChannelCount; c++)
        {
            Output.Buffer.GetChannel(c).Fill(Value + c);
        }
    }
}

internal sealed class HalvingEffect : EffectModule
{
    protected override void Process(AudioBuffer buffer)
    {
        buffer.ApplyGain(0.5f);
    }
}

internal sealed class PassThrough : EffectModule
{
    public Action<AudioModule>? OnProcess { get; set; }

    protected override void Process(AudioBuffer buffer)
    {
        OnProcess?.Invoke(this);
    }
}

/// <summary>
/// Keeps a copy of the last block it received.
/// </summary>
internal sealed class RecordingSink : AudioModule
{
    public RecordingSink(ChannelLayout? layout = null)
    {
        Input = AddInput("Input", layout);
    }

    public InputPort Input { get; }

    public AudioBuffer? Last { get; private set; }

    public Action<AudioModule>? OnProcess { get; set; }

    protected override void Process()
    {
        OnProcess?.Invoke(this);
        Last ??= new AudioBuffer(Format.FrameCount);
        Last.CopyFrom(Input.Buffer);
    }
}

internal sealed class ThrowingSource : AudioModule
{
    public ThrowingSource()
    {
        Output = AddOutput("Output");
    }

    public OutputPort Output { get; }

    protected override void Process()
    {
        throw new InvalidOperationException("Test failure");
    }
}

/// <summary>
/// Applies its volume to the input, like a device output.
/// </summary>
internal sealed class VolumeSink : AudioModule
{
    public VolumeSink()
    {
        Input = AddInput("Input");
    }

    public InputPort Input { get; }

    protected override void Process()
    {
        ApplyVolume(Input.Buffer);
    }
}
