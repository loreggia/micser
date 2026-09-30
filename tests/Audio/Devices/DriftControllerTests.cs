using Micser.Audio.Devices;

namespace Micser.Audio.Tests.Devices;

public class DriftControllerTests
{
    private const int BlockFrames = 480;

    [Test]
    [Arguments(200e-6)]
    [Arguments(-200e-6)]
    [Arguments(0d)]
    public async Task Update_KeepsFillNearTargetAndLearnsClockOffset(double producerOffset)
    {
        // A producer on a clock that's off by producerOffset feeds a consumer that reads BlockFrames * correction frames per block.
        // The producer delivers in bursts of alternating size to simulate device periods that don't line up with the engine blocks.
        var controller = new DriftController(2 * BlockFrames);
        var fill = controller.TargetFill;
        var produced = 0d;
        var consumed = 0d;
        var minFill = double.MaxValue;
        var maxFill = double.MinValue;

        for (var block = 0; block < 100 * 600; block++)
        {
            var burst = block % 2 == 0 ? 0.5 : 1.5;
            produced += BlockFrames * burst * (1 + producerOffset);
            var correction = controller.Update(fill);
            consumed += BlockFrames * correction;
            fill = controller.TargetFill + produced - consumed;

            if (block > 100 * 60)
            {
                minFill = Math.Min(minFill, fill);
                maxFill = Math.Max(maxFill, fill);
            }
        }

        await Assert.That(controller.Correction).IsEqualTo(1 + producerOffset).Within(20e-6);
        await Assert.That(minFill).IsGreaterThan(controller.TargetFill - 1.5 * BlockFrames);
        await Assert.That(maxFill).IsLessThan(controller.TargetFill + 1.5 * BlockFrames);
    }

    [Test]
    public async Task Update_LimitsCorrection()
    {
        var controller = new DriftController(960, maxCorrection: 0.005);

        for (var i = 0; i < 10000; i++)
        {
            controller.Update(100000);
        }

        await Assert.That(controller.Correction).IsEqualTo(1.005).Within(1e-9);
    }
}
