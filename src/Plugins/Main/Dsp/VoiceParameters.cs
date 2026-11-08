namespace Micser.Plugins.Main.Dsp;

/// <summary>
/// How <see cref="VoiceShifter"/> synthesizes the voice.
/// </summary>
internal enum VoiceTexture
{
    /// <summary>
    /// The harmonics as they are, moved by the pitch factor.
    /// </summary>
    Voiced,

    /// <summary>
    /// The harmonics of a fixed note (<see cref="VoiceShifter.RobotFrequency"/> times the pitch factor): a monotone, robotic voice.
    /// </summary>
    Robot,

    /// <summary>
    /// Noise shaped by the envelope instead of the harmonics.
    /// </summary>
    Whisper,
}

/// <summary>
/// How <see cref="VoiceShifter"/> corrects the detected pitch.
/// </summary>
internal enum PitchCorrection
{
    None,

    /// <summary>
    /// Holds the pitch at the speaker's average, so the voice loses its intonation.
    /// </summary>
    Monotone,

    /// <summary>
    /// Moves the pitch to the nearest semitone, as in hard-tuned singing.
    /// </summary>
    NoteSnap,
}

/// <summary>
/// The parameters of a <see cref="VoiceShifter"/> block.
/// </summary>
/// <param name="PitchFactor">The frequency factor of the harmonics, e.g. 2 for one octave up; not used with a target pitch.</param>
/// <param name="FormantFactor">The frequency factor of the spectral envelope; equal to the pitch factor, the formants move with the pitch.
/// </param>
/// <param name="Mix">0 (only the delayed dry signal) .. 1 (only the changed signal), ramped over the block.</param>
/// <param name="Breathiness">0 .. 1: replaces part of the harmonics with noise shaped by the envelope.</param>
/// <param name="Wobble">0 .. 1: the depth of a slow vibrato and tremolo, as in a shaky voice.</param>
/// <param name="Texture">How the voice is synthesized.</param>
/// <param name="TargetPitch">In Hz: moves the speaker's average pitch there, keeping the intonation; 0 shifts by the pitch factor.</param>
/// <param name="Correction">How the detected pitch is corrected.</param>
internal readonly record struct VoiceParameters(
    float PitchFactor,
    float FormantFactor,
    float Mix,
    float Breathiness = 0f,
    float Wobble = 0f,
    VoiceTexture Texture = VoiceTexture.Voiced,
    float TargetPitch = 0f,
    PitchCorrection Correction = PitchCorrection.None
);
