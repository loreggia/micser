using NAudio.Wave;

namespace Micser.Audio.Devices.PipeWire;

/// <summary>
/// SPA audio channel positions (<c>enum spa_audio_channel</c>) and their WAVEFORMATEXTENSIBLE speakers.
/// </summary>
internal static class SpaAudioChannels
{
    public const uint Mono = 2;

    /// <summary>
    /// <c>SPA_AUDIO_CHANNEL_AUX0</c>; positions without a speaker are numbered from here.
    /// </summary>
    public const uint Aux0 = 0x1000;

    private static readonly (string Name, uint Position, Speakers Speaker)[] Channels =
    [
        ("MONO", Mono, Speakers.None),
        ("FL", 3, Speakers.FrontLeft),
        ("FR", 4, Speakers.FrontRight),
        ("FC", 5, Speakers.FrontCenter),
        ("LFE", 6, Speakers.LowFrequency),
        ("SL", 7, Speakers.SideLeft),
        ("SR", 8, Speakers.SideRight),
        ("FLC", 9, Speakers.FrontLeftOfCenter),
        ("FRC", 10, Speakers.FrontRightOfCenter),
        ("RC", 11, Speakers.BackCenter),
        ("RL", 12, Speakers.BackLeft),
        ("RR", 13, Speakers.BackRight),
        ("TC", 14, Speakers.TopCenter),
        ("TFL", 15, Speakers.TopFrontLeft),
        ("TFC", 16, Speakers.TopFrontCenter),
        ("TFR", 17, Speakers.TopFrontRight),
        ("TRL", 18, Speakers.TopBackLeft),
        ("TRC", 19, Speakers.TopBackCenter),
        ("TRR", 20, Speakers.TopBackRight),
    ];

    /// <summary>
    /// The layout to process a device with, and the channel positions to request from PipeWire so that the stream's channels come in that
    /// layout's order. Positions with a speaker are sorted by their speaker bit (the WAVEFORMATEXTENSIBLE order), so the stream's channel
    /// converter reorders e.g. ALSA's 5.1 (FL FR RL RR FC LFE). A device with positions that have no speaker or repeat one is taken channel
    /// by channel in its own order, as a layout without speaker positions.
    /// </summary>
    /// <param name="positions">The device's positions (<c>audio.position</c>), or empty if it has none.</param>
    /// <param name="channelCount">The device's channel count (<c>audio.channels</c>), or 0 if unknown.</param>
    public static (ChannelLayout Layout, uint[] Positions) Negotiate(IReadOnlyList<string> positions, int channelCount)
    {
        if (positions.Count == 0)
        {
            var count = Math.Clamp(channelCount > 0 ? channelCount : 2, 1, AudioModule.MaxChannelCount);
            var layout = ChannelLayout.FromChannelCount(count);
            return (layout, layout.HasSpeakerPositions ? ToPositions(layout.Speakers) : AuxPositions(count));
        }

        if (positions is ["MONO"])
        {
            return (ChannelLayout.Mono, [Mono]);
        }

        var ids = positions.Select(FromName).ToArray();
        var speakers = ids.Select(ToSpeaker).ToArray();
        var mask = speakers.Aggregate(Speakers.None, (all, speaker) => all | speaker);
        var hasSpeakers =
            speakers.All(speaker => speaker != Speakers.None)
            && System.Numerics.BitOperations.PopCount((uint)mask) == speakers.Length;

        return hasSpeakers
            ? (new ChannelLayout(ids.Length, mask), ToPositions(mask))
            : (new ChannelLayout(Math.Min(ids.Length, AudioModule.MaxChannelCount), Speakers.None), ids);
    }

    /// <summary>
    /// The position names (e.g. <c>FL</c>) of a layout with speaker positions, in its channel order; <c>AUX0</c>… otherwise.
    /// </summary>
    public static string[] ToNames(ChannelLayout layout)
    {
        if (!layout.HasSpeakerPositions)
        {
            return Enumerable.Range(0, layout.ChannelCount).Select(i => $"AUX{i}").ToArray();
        }

        return Channels
            .Where(channel => channel.Speaker != Speakers.None && (layout.Speakers & channel.Speaker) != 0)
            .OrderBy(channel => (uint)channel.Speaker)
            .Select(channel => channel.Name)
            .ToArray();
    }

    /// <summary>
    /// Parses <c>audio.position</c>, e.g. <c>FL,FR</c> or <c>[ FL, FR ]</c>.
    /// </summary>
    public static IReadOnlyList<string> ParsePositions(string? value)
    {
        return value == null
            ? []
            : value.Split([',', ' ', '[', ']'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static uint[] AuxPositions(int count)
    {
        return Enumerable.Range(0, count).Select(i => Aux0 + (uint)i).ToArray();
    }

    /// <summary>
    /// The position of a name like <c>FL</c> or <c>AUX3</c>; 0 (<c>SPA_AUDIO_CHANNEL_UNKNOWN</c>) for others.
    /// </summary>
    private static uint FromName(string name)
    {
        foreach (var (channelName, position, _) in Channels)
        {
            if (string.Equals(channelName, name, StringComparison.OrdinalIgnoreCase))
            {
                return position;
            }
        }

        return name.StartsWith("AUX", StringComparison.OrdinalIgnoreCase) && uint.TryParse(name.AsSpan(3), out var aux)
            ? Aux0 + aux
            : 0;
    }

    private static Speakers ToSpeaker(uint position)
    {
        foreach (var (_, channelPosition, speaker) in Channels)
        {
            if (channelPosition == position)
            {
                return speaker;
            }
        }

        return Speakers.None;
    }

    /// <summary>
    /// The positions of the speakers in <paramref name="mask"/>, in the order of their bits.
    /// </summary>
    private static uint[] ToPositions(Speakers mask)
    {
        return Channels
            .Where(channel => channel.Speaker != Speakers.None && (mask & channel.Speaker) != 0)
            .OrderBy(channel => (uint)channel.Speaker)
            .Select(channel => channel.Position)
            .ToArray();
    }
}
