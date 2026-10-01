using Micser.Audio.Devices;

namespace Micser.Audio.Tests.Devices;

public class AdaptiveTargetTests
{
    private const long HoldOff = 10;
    private const long Stable = 100;

    [Test]
    public async Task Update_Dropout_GrowsByStepOncePerIncident()
    {
        var target = new AdaptiveTarget(minimum: 100, step: 50, maximum: 1000, HoldOff, Stable);

        var first = target.Update(dropout: true);
        var sameIncident = target.Update(dropout: true);
        Advance(target, HoldOff);
        var second = target.Update(dropout: true);

        await Assert.That(first).IsTrue();
        await Assert.That(sameIncident).IsFalse();
        await Assert.That(second).IsTrue();
        await Assert.That(target.Value).IsEqualTo(200d);
    }

    [Test]
    public async Task Update_Dropouts_StopAtMaximum()
    {
        var target = new AdaptiveTarget(minimum: 100, step: 50, maximum: 180, HoldOff, Stable);

        for (var i = 0; i < 5; i++)
        {
            target.Update(dropout: true);
            Advance(target, HoldOff);
        }

        await Assert.That(target.Value).IsEqualTo(180d);
    }

    [Test]
    public async Task Update_Stable_ShrinksBackToMinimum()
    {
        var target = new AdaptiveTarget(minimum: 100, step: 50, maximum: 1000, HoldOff, Stable);
        target.Update(dropout: true);
        Advance(target, HoldOff);
        target.Update(dropout: true);

        Advance(target, Stable);
        var afterOnePeriod = target.Value;
        Advance(target, Stable);

        await Assert.That(afterOnePeriod).IsEqualTo(150d);
        await Assert.That(target.Value).IsEqualTo(100d);
    }

    [Test]
    public async Task Update_DropoutSoonAfterShrinking_KeepsTheLevel()
    {
        var target = new AdaptiveTarget(minimum: 100, step: 50, maximum: 1000, HoldOff, Stable);
        target.Update(dropout: true);
        Advance(target, Stable);

        // shrunk to 100, which turns out to be too low
        var shrunk = target.Value;
        Advance(target, HoldOff);
        target.Update(dropout: true);
        Advance(target, 10 * Stable);

        await Assert.That(shrunk).IsEqualTo(100d);
        await Assert.That(target.Value).IsEqualTo(150d);
    }

    private static void Advance(AdaptiveTarget target, long blocks)
    {
        for (var i = 0; i < blocks; i++)
        {
            target.Update(dropout: false);
        }
    }
}
