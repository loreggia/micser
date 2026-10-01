using Micser.Plugins.Main.Modules;

namespace Micser.Plugins.Main.Tests.Modules;

public class RetryBackoffTests
{
    [Test]
    public async Task Attempted_DoublesTheDelayUpTo30Seconds()
    {
        long now = 0;
        var backoff = new RetryBackoff(() => now);
        var delays = new List<long>();

        for (var i = 0; i < 8; i++)
        {
            var attemptedAt = now;
            backoff.Attempted();
            while (!backoff.IsDue)
            {
                now += 100;
            }

            delays.Add(now - attemptedAt);
        }

        await Assert.That(delays).IsEquivalentTo([1000L, 2000L, 4000L, 8000L, 16000L, 30000L, 30000L, 30000L]);
    }

    [Test]
    public async Task IsDue_InitiallyAndAfterReset()
    {
        long now = 0;
        var backoff = new RetryBackoff(() => now);
        var initially = backoff.IsDue;
        backoff.Attempted();
        backoff.Attempted();
        var afterAttempts = backoff.IsDue;

        backoff.Reset();
        var afterReset = backoff.IsDue;
        backoff.Attempted();
        now += 1000;

        await Assert.That(initially).IsTrue();
        await Assert.That(afterAttempts).IsFalse();
        await Assert.That(afterReset).IsTrue();
        await Assert.That(backoff.IsDue).IsTrue();
    }
}
