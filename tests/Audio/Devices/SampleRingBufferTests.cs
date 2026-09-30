using Micser.Audio.Devices;

namespace Micser.Audio.Tests.Devices;

public class SampleRingBufferTests
{
    [Test]
    public async Task Discard_DropsOldestSamples()
    {
        var ring = new SampleRingBuffer(4);
        var destination = new float[1];
        ring.Write([1, 2, 3]);

        ring.Discard(2);
        ring.Read(destination);

        await Assert.That(destination[0]).IsEqualTo(3f);
    }

    [Test]
    public async Task Read_ReturnsOnlyAvailableSamples()
    {
        var ring = new SampleRingBuffer(4);
        ring.Write([1, 2]);

        var read = ring.Read(new float[4]);

        await Assert.That(read).IsEqualTo(2);
        await Assert.That(ring.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Write_StopsWhenFull()
    {
        var ring = new SampleRingBuffer(4);

        var written = ring.Write([1, 2, 3, 4, 5]);

        await Assert.That(written).IsEqualTo(4);
        await Assert.That(ring.Count).IsEqualTo(4);
    }

    [Test]
    public async Task WriteAndRead_WrapAround()
    {
        var ring = new SampleRingBuffer(4);
        var destination = new float[3];

        ring.Write([1, 2, 3]);
        ring.Read(destination);
        ring.Write([4, 5, 6]);
        var read = ring.Read(destination);

        await Assert.That(read).IsEqualTo(3);
        await Assert.That(destination).IsEquivalentTo(new float[] { 4, 5, 6 }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task WriteSilence_WritesZeros()
    {
        var ring = new SampleRingBuffer(4);
        var destination = new float[3];
        ring.Write([1, 2, 3]);
        ring.Read(destination);

        ring.WriteSilence(3);
        ring.Read(destination);

        await Assert.That(destination).All().Satisfy(s => s.IsEqualTo(0f));
    }
}
