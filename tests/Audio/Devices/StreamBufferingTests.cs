using Micser.Audio.Devices;

namespace Micser.Audio.Tests.Devices;

public class StreamBufferingTests
{
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
