namespace Micser.Audio.Tests;

public class AudioGraphTests
{
    private static readonly ProcessingFormat Format = new(48000, 16);

    [Test]
    public async Task Add_RejectsModuleOfAnotherGraph()
    {
        var module = new PassThrough();
        new AudioGraph(Format).Add(module);

        await Assert.That(() => new AudioGraph(Format).Add(module)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Connect_RejectsCycles()
    {
        var graph = new AudioGraph(Format);
        var first = new PassThrough();
        var second = new PassThrough();
        graph.Add(first);
        graph.Add(second);
        graph.Connect(first.Output, second.Input);

        await Assert.That(() => graph.Connect(second.Output, first.Input)).Throws<InvalidOperationException>();
        await Assert.That(() => graph.Connect(first.Output, first.Input)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Connect_RejectsDuplicatesAndForeignModules()
    {
        var graph = new AudioGraph(Format);
        var source = new ConstantSource(ChannelLayout.Mono);
        var sink = new RecordingSink();
        graph.Add(source);
        graph.Add(sink);
        graph.Connect(source.Output, sink.Input);

        await Assert.That(() => graph.Connect(source.Output, sink.Input)).Throws<InvalidOperationException>();
        await Assert.That(() => graph.Connect(source.Output, new RecordingSink().Input)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Disconnect_RemovesConnection()
    {
        var graph = new AudioGraph(Format);
        var source = new ConstantSource(ChannelLayout.Mono);
        var sink = new RecordingSink();
        graph.Add(source);
        graph.Add(sink);
        graph.Connect(source.Output, sink.Input);

        var removed = graph.Disconnect(source.Output, sink.Input);
        graph.Process();

        await Assert.That(removed).IsTrue();
        await Assert.That(sink.Last!.Layout).IsEqualTo(ChannelLayout.None);
    }

    [Test]
    public async Task Effect_WhenBypassed_PassesAudioThrough()
    {
        var graph = new AudioGraph(Format);
        var source = new ConstantSource(ChannelLayout.Mono);
        var effect = new HalvingEffect();
        var sink = new RecordingSink();
        graph.Add(source);
        graph.Add(effect);
        graph.Add(sink);
        graph.Connect(source.Output, effect.Input);
        graph.Connect(effect.Output, sink.Input);

        graph.Process();
        var processed = sink.Last!.GetChannel(0)[0];
        effect.IsBypassed = true;
        graph.Process();
        var bypassed = sink.Last!.GetChannel(0)[0];

        await Assert.That(processed).IsEqualTo(0.5f);
        await Assert.That(bypassed).IsEqualTo(1f);
    }

    [Test]
    public async Task Muted_OutputsSilenceAfterRamp()
    {
        var graph = new AudioGraph(Format);
        var source = new ConstantSource(ChannelLayout.Mono) { IsMuted = true };
        var sink = new RecordingSink();
        graph.Add(source);
        graph.Add(sink);
        graph.Connect(source.Output, sink.Input);

        graph.Process();
        graph.Process();

        await Assert.That(sink.Last!.GetChannel(0).ToArray()).All().Satisfy(s => s.IsEqualTo(0f));
    }

    [Test]
    public async Task Process_FailingModule_OutputsSilenceAndOthersContinue()
    {
        var graph = new AudioGraph(Format);
        var failing = new ThrowingSource();
        var source = new ConstantSource(ChannelLayout.Mono, 0.25f);
        var sink = new RecordingSink();
        graph.Add(failing);
        graph.Add(source);
        graph.Add(sink);
        graph.Connect(failing.Output, sink.Input);
        graph.Connect(source.Output, sink.Input);

        graph.Process();

        await Assert.That(sink.Last!.Layout).IsEqualTo(ChannelLayout.Mono);
        await Assert.That(sink.Last.GetChannel(0)[0]).IsEqualTo(0.25f);
    }

    [Test]
    public async Task Process_RunsModulesInDependencyOrder()
    {
        var graph = new AudioGraph(Format);
        var order = new List<AudioModule>();
        var sink = new RecordingSink { OnProcess = order.Add };
        var effect = new PassThrough { OnProcess = order.Add };
        var source = new ConstantSource(ChannelLayout.Stereo) { OnProcess = order.Add };
        graph.Add(sink);
        graph.Add(effect);
        graph.Add(source);
        graph.Connect(effect.Output, sink.Input);
        graph.Connect(source.Output, effect.Input);

        graph.Process();

        await Assert.That(order).IsEquivalentTo(new AudioModule[] { source, effect, sink }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(sink.Last!.GetChannel(1)[0]).IsEqualTo(2f);
    }

    [Test]
    public async Task Process_SumsAllConnectionsOfAnInput()
    {
        var graph = new AudioGraph(Format);
        var first = new ConstantSource(ChannelLayout.Mono, 0.25f);
        var second = new ConstantSource(ChannelLayout.Mono, 0.5f);
        var sink = new RecordingSink();
        graph.Add(first);
        graph.Add(second);
        graph.Add(sink);
        graph.Connect(first.Output, sink.Input);
        graph.Connect(second.Output, sink.Input);

        graph.Process();

        await Assert.That(sink.Last!.GetChannel(0)[0]).IsEqualTo(0.75f);
    }

    [Test]
    public async Task Process_UnconnectedInput_IsEmpty()
    {
        var graph = new AudioGraph(Format);
        var sink = new RecordingSink();
        graph.Add(sink);

        graph.Process();

        await Assert.That(sink.Last!.Layout).IsEqualTo(ChannelLayout.None);
    }

    [Test]
    public async Task Process_WithFixedLayout_ConvertsSources()
    {
        var graph = new AudioGraph(Format);
        var stereo = new ConstantSource(ChannelLayout.Stereo, 0.5f);
        var sink = new RecordingSink(ChannelLayout.Mono);
        graph.Add(stereo);
        graph.Add(sink);
        graph.Connect(stereo.Output, sink.Input);

        graph.Process();

        await Assert.That(sink.Last!.Layout).IsEqualTo(ChannelLayout.Mono);
        await Assert.That(sink.Last.GetChannel(0)[0]).IsEqualTo(1f);
    }

    [Test]
    public async Task Process_WithoutFixedLayout_MixesIntoWidestSourceLayout()
    {
        var graph = new AudioGraph(Format);
        var mono = new ConstantSource(ChannelLayout.Mono, 0.25f);
        var stereo = new ConstantSource(ChannelLayout.Stereo, 0.5f);
        var sink = new RecordingSink();
        graph.Add(mono);
        graph.Add(stereo);
        graph.Add(sink);
        graph.Connect(mono.Output, sink.Input);
        graph.Connect(stereo.Output, sink.Input);

        graph.Process();

        await Assert.That(sink.Last!.Layout).IsEqualTo(ChannelLayout.Stereo);
        await Assert.That(sink.Last.GetChannel(0)[0]).IsEqualTo(0.75f);
        await Assert.That(sink.Last.GetChannel(1)[0]).IsEqualTo(1.75f);
    }

    [Test]
    public async Task Remove_DropsConnectionsAndStopsProcessing()
    {
        var graph = new AudioGraph(Format);
        var processed = 0;
        var source = new ConstantSource(ChannelLayout.Mono) { OnProcess = _ => processed++ };
        var sink = new RecordingSink();
        graph.Add(source);
        graph.Add(sink);
        graph.Connect(source.Output, sink.Input);

        graph.Remove(source);
        graph.Process();

        await Assert.That(processed).IsEqualTo(0);
        await Assert.That(graph.Connections).IsEmpty();
        await Assert.That(sink.Last!.Layout).IsEqualTo(ChannelLayout.None);
        await Assert.That(source.IsAttached).IsFalse();
    }

    [Test]
    public async Task Volume_RampsOverOneBlockThenStays()
    {
        var graph = new AudioGraph(Format);
        var source = new ConstantSource(ChannelLayout.Mono) { Volume = 0.5f };
        var sink = new RecordingSink();
        graph.Add(source);
        graph.Add(sink);
        graph.Connect(source.Output, sink.Input);

        graph.Process();
        var firstBlock = sink.Last!.GetChannel(0).ToArray();
        graph.Process();
        var secondBlock = sink.Last!.GetChannel(0).ToArray();

        await Assert.That(firstBlock[0]).IsGreaterThan(0.5f);
        await Assert.That(firstBlock[^1]).IsEqualTo(0.5f).Within(1e-6f);
        await Assert.That(secondBlock).All().Satisfy(s => s.IsEqualTo(0.5f));
    }
}
