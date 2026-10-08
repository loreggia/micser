using NAudio.Wave;

namespace Micser.Audio;

/// <summary>
/// Mixes a buffer of one channel layout into a buffer of another layout.
/// <list type="bullet">
/// <item>Mono sources go to the front center speaker if the target has one, otherwise to front left and right (or
/// to all channels of a layout without speaker positions).</item>
/// <item>Mono targets receive the average of all source channels except LFE.</item>
/// <item>Between layouts with speaker positions, matching speakers map 1:1 and missing speakers fold into their
/// neighbours (e.g. center into front left/right at -3 dB, back into side or front). LFE is dropped.</item>
/// <item>Otherwise channels are mapped by index; surplus channels are dropped.</item>
/// </list>
/// It can also take a single source channel (mixed in as a mono source) or add to a single target channel (mixed down as for a mono
/// target), or both (1:1).
/// </summary>
public sealed class ChannelMixer
{
    private const float MinusThreeDb = 0.70710677f;

    private static readonly Dictionary<Speakers, (Speakers Speaker, float Gain)[][]> Fallbacks = new()
    {
        [Speakers.FrontCenter] = [[(Speakers.FrontLeft, MinusThreeDb), (Speakers.FrontRight, MinusThreeDb)]],
        [Speakers.FrontLeftOfCenter] = [[(Speakers.FrontLeft, 1f)]],
        [Speakers.FrontRightOfCenter] = [[(Speakers.FrontRight, 1f)]],
        [Speakers.SideLeft] = [[(Speakers.BackLeft, 1f)], [(Speakers.FrontLeft, MinusThreeDb)]],
        [Speakers.SideRight] = [[(Speakers.BackRight, 1f)], [(Speakers.FrontRight, MinusThreeDb)]],
        [Speakers.BackLeft] = [[(Speakers.SideLeft, 1f)], [(Speakers.FrontLeft, MinusThreeDb)]],
        [Speakers.BackRight] = [[(Speakers.SideRight, 1f)], [(Speakers.FrontRight, MinusThreeDb)]],
        [Speakers.BackCenter] = [[(Speakers.BackLeft, MinusThreeDb), (Speakers.BackRight, MinusThreeDb)], [(Speakers.SideLeft, MinusThreeDb), (Speakers.SideRight, MinusThreeDb)], [(Speakers.FrontLeft, 0.5f), (Speakers.FrontRight, 0.5f)]],
        [Speakers.TopCenter] = [[(Speakers.FrontLeft, 0.5f), (Speakers.FrontRight, 0.5f)]],
        [Speakers.TopFrontLeft] = [[(Speakers.FrontLeft, 1f)]],
        [Speakers.TopFrontCenter] = [[(Speakers.FrontCenter, 1f)]],
        [Speakers.TopFrontRight] = [[(Speakers.FrontRight, 1f)]],
        [Speakers.TopBackLeft] = [[(Speakers.BackLeft, 1f)]],
        [Speakers.TopBackCenter] = [[(Speakers.BackCenter, 1f)]],
        [Speakers.TopBackRight] = [[(Speakers.BackRight, 1f)]],
    };

    private readonly Route[] _routes;

    public ChannelMixer(ChannelLayout source, ChannelLayout target)
        : this(source, target, null, null)
    {
    }

    /// <summary>
    /// Creates a mixer that takes only one channel of the source, or adds only to one channel of the target. A source channel is mixed in
    /// like a mono source; for a target channel, the source is mixed down like for a mono target.
    /// </summary>
    /// <param name="sourceChannel">The source channel to take, or null for all.</param>
    /// <param name="targetChannel">The target channel to add to, or null for all.</param>
    public ChannelMixer(ChannelLayout source, ChannelLayout target, int? sourceChannel, int? targetChannel)
    {
        if (sourceChannel is { } sourceIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(sourceIndex, nameof(sourceChannel));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(sourceIndex, source.ChannelCount, nameof(sourceChannel));
        }

        if (targetChannel is { } targetIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(targetIndex, nameof(targetChannel));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(targetIndex, target.ChannelCount, nameof(targetChannel));
        }

        Source = source;
        Target = target;
        SourceChannel = sourceChannel;
        TargetChannel = targetChannel;
        _routes = (sourceChannel, targetChannel) switch
        {
            ({ } s, { } t) => [new Route(s, t, 1f)],
            ({ } s, null) => [.. CreateRoutes(ChannelLayout.Mono, target).Select(r => r with { SourceChannel = s })],
            (null, { } t) => [.. CreateRoutes(source, ChannelLayout.Mono).Select(r => r with { TargetChannel = t })],
            _ => CreateRoutes(source, target),
        };
    }

    public ChannelLayout Source { get; }

    /// <summary>
    /// The only source channel taken, or null for all.
    /// </summary>
    public int? SourceChannel { get; }

    public ChannelLayout Target { get; }

    /// <summary>
    /// The only target channel added to, or null for all.
    /// </summary>
    public int? TargetChannel { get; }

    /// <summary>
    /// Gets the gain applied from a source channel to a target channel.
    /// </summary>
    public float GetGain(int sourceChannel, int targetChannel)
    {
        var gain = 0f;
        foreach (var route in _routes)
        {
            if (route.SourceChannel == sourceChannel && route.TargetChannel == targetChannel)
            {
                gain += route.Gain;
            }
        }

        return gain;
    }

    /// <summary>
    /// Adds the converted samples of <paramref name="source"/> to <paramref name="target"/>.
    /// </summary>
    public void MixInto(AudioBuffer source, AudioBuffer target)
    {
        if (source.Layout != Source || target.Layout != Target)
        {
            throw new ArgumentException("The buffer layouts don't match the mixer.");
        }

        foreach (var route in _routes)
        {
            var sourceChannel = source.GetChannel(route.SourceChannel);
            var targetChannel = target.GetChannel(route.TargetChannel);
            var gain = route.Gain;

            if (gain == 1f)
            {
                for (var i = 0; i < targetChannel.Length; i++)
                {
                    targetChannel[i] += sourceChannel[i];
                }
            }
            else
            {
                for (var i = 0; i < targetChannel.Length; i++)
                {
                    targetChannel[i] += sourceChannel[i] * gain;
                }
            }
        }
    }

    private static void AddMonoSourceRoutes(ChannelLayout target, List<Route> routes)
    {
        if (target.HasSpeakerPositions)
        {
            var center = target.IndexOf(Speakers.FrontCenter);
            if (center >= 0)
            {
                routes.Add(new Route(0, center, 1f));
                return;
            }

            var left = target.IndexOf(Speakers.FrontLeft);
            var right = target.IndexOf(Speakers.FrontRight);
            if (left >= 0 || right >= 0)
            {
                if (left >= 0)
                {
                    routes.Add(new Route(0, left, 1f));
                }

                if (right >= 0)
                {
                    routes.Add(new Route(0, right, 1f));
                }

                return;
            }
        }

        for (var c = 0; c < target.ChannelCount; c++)
        {
            routes.Add(new Route(0, c, 1f));
        }
    }

    private static void AddMonoTargetRoutes(ChannelLayout source, List<Route> routes)
    {
        var channels = Enumerable.Range(0, source.ChannelCount)
            .Where(c => source.GetSpeaker(c) != Speakers.LowFrequency)
            .ToArray();

        foreach (var c in channels)
        {
            routes.Add(new Route(c, 0, 1f / channels.Length));
        }
    }

    private static Route[] CreateRoutes(ChannelLayout source, ChannelLayout target)
    {
        var routes = new List<Route>();

        if (source.ChannelCount == 0 || target.ChannelCount == 0)
        {
            return [];
        }

        if (source == target)
        {
            for (var c = 0; c < source.ChannelCount; c++)
            {
                routes.Add(new Route(c, c, 1f));
            }
        }
        else if (source.ChannelCount == 1)
        {
            AddMonoSourceRoutes(target, routes);
        }
        else if (target.ChannelCount == 1)
        {
            AddMonoTargetRoutes(source, routes);
        }
        else if (source.HasSpeakerPositions && target.HasSpeakerPositions)
        {
            for (var c = 0; c < source.ChannelCount; c++)
            {
                foreach (var (speaker, gain) in Resolve(source.GetSpeaker(c), 1f, target, 0))
                {
                    routes.Add(new Route(c, target.IndexOf(speaker), gain));
                }
            }
        }
        else
        {
            for (var c = 0; c < Math.Min(source.ChannelCount, target.ChannelCount); c++)
            {
                routes.Add(new Route(c, c, 1f));
            }
        }

        return [.. routes];
    }

    private static IEnumerable<(Speakers Speaker, float Gain)> Resolve(Speakers speaker, float gain, ChannelLayout target, int depth)
    {
        if (target.IndexOf(speaker) >= 0)
        {
            return [(speaker, gain)];
        }

        if (depth > 3 || !Fallbacks.TryGetValue(speaker, out var alternatives))
        {
            return [];
        }

        foreach (var alternative in alternatives)
        {
            var resolved = alternative
                .SelectMany(t => Resolve(t.Speaker, gain * t.Gain, target, depth + 1))
                .ToArray();

            if (resolved.Length > 0)
            {
                return resolved;
            }
        }

        return [];
    }

    private readonly record struct Route(int SourceChannel, int TargetChannel, float Gain);
}
