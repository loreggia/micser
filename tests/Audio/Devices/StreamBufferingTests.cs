using Micser.Audio.Devices;

namespace Micser.Audio.Tests.Devices;

public class StreamBufferingTests
{
    [Test]
    [Arguments(0L, 50L, false)]
    [Arguments(1_000L, 1_050L, false)]
    [Arguments(1_000L, 1_200L, true)]
    [Arguments(0L, 1_000L, true)]
    public async Task IsIdle_AfterNoActivityFor100Milliseconds(long lastActivity, long now, bool expected)
    {
        await Assert.That(StreamBuffering.IsIdle(lastActivity, now)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(500L, true)]
    [Arguments(999L, true)]
    [Arguments(1_000L, false)]
    public async Task IsSettling_DuringTheFirstSecond(long now, bool expected)
    {
        await Assert.That(StreamBuffering.IsSettling(openedAt: 0, now)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(0L, 1_000L, false)]
    [Arguments(0L, 9_000L, false)]
    [Arguments(0L, 11_000L, true)]
    [Arguments(5_000L, 6_000L, false)]
    [Arguments(5_000L, 7_500L, true)]
    public async Task IsStalled_UsesStartTimeoutUntilFirstActivity(long lastActivity, long now, bool expected)
    {
        await Assert.That(StreamBuffering.IsStalled(openedAt: 0, lastActivity, now)).IsEqualTo(expected);
    }
}
