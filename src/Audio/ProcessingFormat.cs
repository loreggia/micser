namespace Micser.Audio;

/// <summary>
/// The sample rate and block size the graph is processed at.
/// </summary>
public readonly record struct ProcessingFormat(int SampleRate, int FrameCount)
{
    public static readonly ProcessingFormat Default = new(48000, 240);

    public TimeSpan BlockDuration => TimeSpan.FromSeconds((double)FrameCount / SampleRate);
}
