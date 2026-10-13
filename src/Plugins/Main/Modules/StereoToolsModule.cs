using System.ComponentModel.DataAnnotations;
using Micser.Audio;

namespace Micser.Plugins.Main.Modules;

public enum StereoMode
{
    /// <summary>
    /// Left and right stay on their side.
    /// </summary>
    Stereo,

    /// <summary>
    /// Left and right change sides.
    /// </summary>
    Swap,

    /// <summary>
    /// Left on both sides, e.g. for a microphone that only uses the left channel.
    /// </summary>
    LeftToBoth,

    /// <summary>
    /// Right on both sides.
    /// </summary>
    RightToBoth,

    /// <summary>
    /// The average of left and right on both sides.
    /// </summary>
    Mono,
}

/// <param name="Mode">Which input channels go to which side.</param>
/// <param name="Width">Stereo width: 0 is mono, 1 unchanged, 2 the side signal doubled.</param>
/// <param name="Balance">-1 is left only, 0 both sides unchanged, 1 right only.</param>
/// <param name="InvertLeft">Inverts the polarity of the left input channel.</param>
/// <param name="InvertRight">Inverts the polarity of the right input channel.</param>
public sealed record StereoToolsState(
    StereoMode Mode = StereoMode.Stereo,
    [Range(0f, 2f)] float Width = 1f,
    [Range(-1f, 1f)] float Balance = 0f,
    bool InvertLeft = false,
    bool InvertRight = false
);

/// <summary>
/// Changes the first two channels (front left and right): polarity, then <see cref="Mode"/>, <see cref="Width"/> and <see cref="Balance"/>.
/// Other channels and mono audio pass unchanged. Changes ramp over one block.
/// </summary>
public class StereoToolsModule : EffectModule, IStatefulModule<StereoToolsState>
{
    private Matrix _applied = Matrix.Identity;

    /// <summary>
    /// -1 is left only, 0 both sides unchanged, 1 right only.
    /// </summary>
    public float Balance { get; set; }

    /// <summary>
    /// Inverts the polarity of the left input channel.
    /// </summary>
    public bool InvertLeft { get; set; }

    /// <summary>
    /// Inverts the polarity of the right input channel.
    /// </summary>
    public bool InvertRight { get; set; }

    public StereoMode Mode { get; set; } = StereoMode.Stereo;

    /// <summary>
    /// Stereo width: 0 is mono, 1 unchanged, 2 the side signal doubled.
    /// </summary>
    public float Width { get; set; } = 1f;

    public StereoToolsState GetState()
    {
        return new StereoToolsState(Mode, Width, Balance, InvertLeft, InvertRight);
    }

    public void SetState(StereoToolsState state)
    {
        (Mode, Width, Balance, InvertLeft, InvertRight) = (
            state.Mode,
            state.Width,
            state.Balance,
            state.InvertLeft,
            state.InvertRight
        );
    }

    protected override void Process(AudioBuffer buffer)
    {
        if (buffer.ChannelCount < 2)
        {
            return;
        }

        var target = ComputeMatrix(Mode, Width, Balance, InvertLeft, InvertRight);
        var start = _applied;
        _applied = target;
        if (start == Matrix.Identity && target == Matrix.Identity)
        {
            return;
        }

        var left = buffer.GetChannel(0);
        var right = buffer.GetChannel(1);
        var step = 1f / left.Length;
        for (var i = 0; i < left.Length; i++)
        {
            var m = start == target ? target : Matrix.Lerp(start, target, (i + 1) * step);
            var l = left[i];
            var r = right[i];
            left[i] = (m.LeftFromLeft * l) + (m.LeftFromRight * r);
            right[i] = (m.RightFromLeft * l) + (m.RightFromRight * r);
        }
    }

    private static Matrix ComputeMatrix(StereoMode mode, float width, float balance, bool invertLeft, bool invertRight)
    {
        var pl = invertLeft ? -1f : 1f;
        var pr = invertRight ? -1f : 1f;
        var routing = mode switch
        {
            StereoMode.Swap => new Matrix(0f, pr, pl, 0f),
            StereoMode.LeftToBoth => new Matrix(pl, 0f, pl, 0f),
            StereoMode.RightToBoth => new Matrix(0f, pr, 0f, pr),
            StereoMode.Mono => new Matrix(pl / 2f, pr / 2f, pl / 2f, pr / 2f),
            _ => new Matrix(pl, 0f, 0f, pr),
        };

        // mid/side: M = (L + R) / 2, S = (L - R) / 2 * width, L' = M + S, R' = M - S
        var w = Math.Clamp(width, 0f, 2f);
        var same = (1f + w) / 2f;
        var other = (1f - w) / 2f;
        var widened = new Matrix(same, other, other, same) * routing;

        var b = Math.Clamp(balance, -1f, 1f);
        var leftGain = Math.Min(1f, 1f - b);
        var rightGain = Math.Min(1f, 1f + b);
        return new Matrix(
            leftGain * widened.LeftFromLeft,
            leftGain * widened.LeftFromRight,
            rightGain * widened.RightFromLeft,
            rightGain * widened.RightFromRight
        );
    }

    private readonly record struct Matrix(
        float LeftFromLeft,
        float LeftFromRight,
        float RightFromLeft,
        float RightFromRight
    )
    {
        public static readonly Matrix Identity = new(1f, 0f, 0f, 1f);

        public static Matrix Lerp(Matrix from, Matrix to, float t)
        {
            return new Matrix(
                from.LeftFromLeft + ((to.LeftFromLeft - from.LeftFromLeft) * t),
                from.LeftFromRight + ((to.LeftFromRight - from.LeftFromRight) * t),
                from.RightFromLeft + ((to.RightFromLeft - from.RightFromLeft) * t),
                from.RightFromRight + ((to.RightFromRight - from.RightFromRight) * t)
            );
        }

        public static Matrix operator *(Matrix a, Matrix b)
        {
            return new Matrix(
                (a.LeftFromLeft * b.LeftFromLeft) + (a.LeftFromRight * b.RightFromLeft),
                (a.LeftFromLeft * b.LeftFromRight) + (a.LeftFromRight * b.RightFromRight),
                (a.RightFromLeft * b.LeftFromLeft) + (a.RightFromRight * b.RightFromLeft),
                (a.RightFromLeft * b.LeftFromRight) + (a.RightFromRight * b.RightFromRight)
            );
        }
    }
}
