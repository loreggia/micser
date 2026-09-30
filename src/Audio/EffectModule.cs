namespace Micser.Audio;

/// <summary>
/// A module with one input and one output of the same layout that processes the audio in place.
/// </summary>
public abstract class EffectModule : AudioModule
{
    protected EffectModule()
    {
        Input = AddInput("Input");
        Output = AddOutput("Output");
    }

    public InputPort Input { get; }

    /// <summary>
    /// Passes the audio through unprocessed. Volume and mute still apply.
    /// </summary>
    public bool IsBypassed { get; set; }

    public OutputPort Output { get; }

    protected sealed override void Process()
    {
        var buffer = Output.Buffer;
        buffer.CopyFrom(Input.Buffer);

        if (!IsBypassed && buffer.ChannelCount > 0)
        {
            Process(buffer);
        }
    }

    /// <summary>
    /// Processes one block in place. The layout may differ from the previous block.
    /// </summary>
    protected abstract void Process(AudioBuffer buffer);
}
