using System.Numerics;
using NAudio.Wave;

namespace Micser.Audio;

/// <summary>
/// The channels of an audio buffer. When <see cref="Speakers"/> has exactly <see cref="ChannelCount"/> bits set,
/// channel <c>i</c> is the <c>i</c>-th lowest speaker bit (WAVEFORMATEXTENSIBLE order); otherwise the channels have no
/// speaker positions and are mapped by index.
/// </summary>
public readonly record struct ChannelLayout(int ChannelCount, Speakers Speakers)
{
    public static readonly ChannelLayout None = new(0, Speakers.None);
    public static readonly ChannelLayout Mono = new(1, Speakers.Mono);
    public static readonly ChannelLayout Stereo = new(2, Speakers.Stereo);
    public static readonly ChannelLayout Quad = new(4, Speakers.Quad);
    public static readonly ChannelLayout Surround51 = new(6, Speakers.Surround51);
    public static readonly ChannelLayout Surround71 = new(8, Speakers.Surround71);

    public bool HasSpeakerPositions => ChannelCount > 0 && BitOperations.PopCount((uint)Speakers) == ChannelCount;

    public static ChannelLayout FromChannelCount(int channelCount)
    {
        return channelCount switch
        {
            0 => None,
            1 => Mono,
            2 => Stereo,
            4 => Quad,
            6 => Surround51,
            8 => Surround71,
            _ => new ChannelLayout(channelCount, Speakers.None),
        };
    }

    public static ChannelLayout FromWaveFormat(WaveFormat format)
    {
        if (format is WaveFormatExtensible extensible)
        {
            var layout = new ChannelLayout(format.Channels, (Speakers)extensible.ChannelMask);
            if (layout.HasSpeakerPositions)
            {
                return layout;
            }
        }

        return FromChannelCount(format.Channels);
    }

    /// <summary>
    /// Returns the speaker of a channel, or <see cref="Speakers.None"/> if the layout has no speaker positions.
    /// </summary>
    public Speakers GetSpeaker(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, ChannelCount);

        if (!HasSpeakerPositions)
        {
            return Speakers.None;
        }

        var mask = (uint)Speakers;
        for (var i = 0; i < channel; i++)
        {
            mask &= mask - 1;
        }

        return (Speakers)(mask & (uint)-(int)mask);
    }

    /// <summary>
    /// Returns the channel index of a speaker, or -1 if the layout doesn't contain it.
    /// </summary>
    public int IndexOf(Speakers speaker)
    {
        if (!HasSpeakerPositions || (Speakers & speaker) == 0 || BitOperations.PopCount((uint)speaker) != 1)
        {
            return -1;
        }

        return BitOperations.PopCount((uint)Speakers & ((uint)speaker - 1));
    }

    public override string ToString()
    {
        return HasSpeakerPositions ? $"{ChannelCount}ch ({Speakers})" : $"{ChannelCount}ch";
    }
}
