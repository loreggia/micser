namespace Micser.Audio.Tests;

public class AudioEngineTests
{
    [Test]
    public async Task Statistics_CountTheAllocationsOfTheAudioThread()
    {
        var graph = new AudioGraph(new ProcessingFormat(48000, 48));
        var allocating = new ConstantSource(ChannelLayout.Stereo) { OnProcess = _ => GC.KeepAlive(new byte[1000]) };
        graph.Add(allocating);
        using var engine = new AudioEngine(graph);

        engine.Start();
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (engine.Statistics.Blocks < 3 && DateTime.UtcNow < timeout)
        {
            await Task.Delay(10);
        }

        engine.Stop();
        var statistics = engine.Statistics;

        await Assert.That(statistics.AllocatingBlocks).IsGreaterThanOrEqualTo(3);
        await Assert.That(statistics.AllocatedBytes).IsGreaterThanOrEqualTo(statistics.AllocatingBlocks * 1000);
    }
}
