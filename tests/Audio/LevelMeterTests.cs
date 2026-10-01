namespace Micser.Audio.Tests;

public class LevelMeterTests
{
    private static readonly ProcessingFormat Format = new(48000, 240);

    [Test]
    public async Task Measure_IgnoresNaN()
    {
        var meter = new LevelMeter(Format);
        var buffer = new AudioBuffer(Format.FrameCount, ChannelLayout.Mono);
        buffer.GetChannel(0).Fill(0.5f);
        buffer.GetChannel(0)[10] = float.NaN;
        meter.Measure(buffer);
        buffer.GetChannel(0).Fill(0.5f);
        meter.Measure(buffer);

        var level = meter.Read()[0];

        await Assert.That(level.Peak).IsEqualTo(0.5f);
        await Assert.That(float.IsFinite(level.Rms)).IsTrue();
    }

    [Test]
    public async Task Measure_SineWave_GivesPeakAndRms()
    {
        var meter = new LevelMeter(Format);
        var buffer = new AudioBuffer(Format.FrameCount, ChannelLayout.Stereo);

        // 2 s, so the smoothed RMS settles
        for (var block = 0; block < 400; block++)
        {
            for (var i = 0; i < Format.FrameCount; i++)
            {
                var sample = MathF.Sin(2 * MathF.PI * 1000 * ((block * Format.FrameCount) + i) / Format.SampleRate);
                buffer.GetChannel(0)[i] = 0.5f * sample;
                buffer.GetChannel(1)[i] = 0.25f * sample;
            }

            meter.Measure(buffer);
        }

        var levels = meter.Read();

        await Assert.That(levels.Length).IsEqualTo(2);
        await Assert.That(levels[0].Peak).IsEqualTo(0.5f).Within(0.001f);
        await Assert.That(levels[0].Rms).IsEqualTo(0.5f / MathF.Sqrt(2)).Within(0.005f);
        await Assert.That(levels[1].Peak).IsEqualTo(0.25f).Within(0.001f);
        await Assert.That(levels[1].Rms).IsEqualTo(0.25f / MathF.Sqrt(2)).Within(0.005f);
    }

    [Test]
    public async Task Read_ResetsPeaks()
    {
        var meter = new LevelMeter(Format);
        var buffer = new AudioBuffer(Format.FrameCount, ChannelLayout.Mono);
        buffer.GetChannel(0).Fill(0.8f);
        meter.Measure(buffer);
        buffer.GetChannel(0).Fill(-0.1f);
        meter.Measure(buffer);

        var first = meter.Read();
        meter.Measure(buffer);
        var second = meter.Read();

        await Assert.That(first[0].Peak).IsEqualTo(0.8f);
        await Assert.That(second[0].Peak).IsEqualTo(0.1f);
    }

    [Test]
    public async Task Read_WithoutNewBlocks_ReturnsNoChannels()
    {
        var meter = new LevelMeter(Format);
        var buffer = new AudioBuffer(Format.FrameCount, ChannelLayout.Mono);
        meter.Measure(buffer);
        meter.Read();

        await Assert.That(meter.Read()).IsEmpty();
        await Assert.That(new LevelMeter(Format).Read()).IsEmpty();
    }

    [Test]
    public async Task ReadLevels_MeasuresOutputsAfterVolume()
    {
        var graph = new AudioGraph(Format);
        var source = new ConstantSource(ChannelLayout.Stereo, 0.5f) { Volume = 0.5f };
        var muted = new ConstantSource(ChannelLayout.Mono) { IsMuted = true };
        graph.Add(source);
        graph.Add(muted);

        // the first block ramps the volume
        graph.Process();
        source.ReadLevels();
        muted.ReadLevels();
        graph.Process();
        var levels = source.ReadLevels();
        var mutedLevels = muted.ReadLevels();

        await Assert.That(levels.Length).IsEqualTo(1);
        await Assert.That(levels[0].Port).IsEqualTo("Output");
        await Assert.That(levels[0].Channels.Select(c => c.Peak)).IsEquivalentTo([0.25f, 0.75f]);
        await Assert.That(mutedLevels[0].Channels[0].Peak).IsEqualTo(0f);
    }

    [Test]
    public async Task ReadLevels_ModuleWithoutOutputs_MeasuresWhatItApplied()
    {
        var graph = new AudioGraph(Format);
        var source = new ConstantSource(ChannelLayout.Mono, 0.8f);
        var sink = new VolumeSink { Volume = 0.5f };
        graph.Add(source);
        graph.Add(sink);
        graph.Connect(source.Output, sink.Input);

        graph.Process();
        sink.ReadLevels();
        graph.Process();
        var levels = sink.ReadLevels();

        await Assert.That(levels.Length).IsEqualTo(1);
        await Assert.That(levels[0].Port).IsNull();
        await Assert.That(levels[0].Channels[0].Peak).IsEqualTo(0.4f);
        await Assert.That(new VolumeSink().ReadLevels()).IsEmpty();
    }
}
