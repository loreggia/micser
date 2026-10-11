using Micser.Audio.Devices.PipeWire;
using NAudio.Wave;
using TUnit.Assertions.Enums;

namespace Micser.Audio.Tests.Devices.PipeWire;

public class SpaAudioChannelsTests
{
    [Test]
    public async Task Negotiate_Stereo()
    {
        var (layout, positions) = SpaAudioChannels.Negotiate(["FL", "FR"], 2);

        await Assert.That(layout).IsEqualTo(ChannelLayout.Stereo);
        await Assert.That(positions).IsEquivalentTo((uint[])[3, 4]);
    }

    [Test]
    public async Task Negotiate_Mono()
    {
        var (layout, positions) = SpaAudioChannels.Negotiate(["MONO"], 1);

        await Assert.That(layout).IsEqualTo(ChannelLayout.Mono);
        await Assert.That(positions).IsEquivalentTo((uint[])[SpaAudioChannels.Mono]);
    }

    [Test]
    public async Task Negotiate_SortsAlsaSurroundIntoSpeakerOrder()
    {
        var (layout, positions) = SpaAudioChannels.Negotiate(["FL", "FR", "RL", "RR", "FC", "LFE"], 6);

        await Assert
            .That(layout)
            .IsEqualTo(
                new ChannelLayout(
                    6,
                    Speakers.FrontLeft
                        | Speakers.FrontRight
                        | Speakers.FrontCenter
                        | Speakers.LowFrequency
                        | Speakers.BackLeft
                        | Speakers.BackRight
                )
            );

        // FL FR FC LFE RL RR
        await Assert.That(positions).IsEquivalentTo((uint[])[3, 4, 5, 6, 12, 13], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Negotiate_KeepsTheOrderOfPositionsWithoutSpeakers()
    {
        var (layout, positions) = SpaAudioChannels.Negotiate(["AUX0", "AUX1", "AUX2"], 3);

        await Assert.That(layout).IsEqualTo(new ChannelLayout(3, Speakers.None));
        await Assert.That(positions).IsEquivalentTo((uint[])[0x1000, 0x1001, 0x1002], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Negotiate_TakesRepeatedSpeakersChannelByChannel()
    {
        var (layout, positions) = SpaAudioChannels.Negotiate(["FL", "FL"], 2);

        await Assert.That(layout).IsEqualTo(new ChannelLayout(2, Speakers.None));
        await Assert.That(positions).IsEquivalentTo((uint[])[3, 3]);
    }

    [Test]
    [Arguments(2)]
    [Arguments(6)]
    public async Task Negotiate_WithoutPositions_UsesTheStandardLayout(int channels)
    {
        var (layout, positions) = SpaAudioChannels.Negotiate([], channels);

        await Assert.That(layout).IsEqualTo(ChannelLayout.FromChannelCount(channels));
        await Assert.That(positions.Length).IsEqualTo(channels);
    }

    [Test]
    public async Task Negotiate_WithoutPositionsOrChannels_IsStereo()
    {
        var (layout, _) = SpaAudioChannels.Negotiate([], 0);

        await Assert.That(layout).IsEqualTo(ChannelLayout.Stereo);
    }

    [Test]
    [Arguments("FL,FR")]
    [Arguments("[ FL, FR ]")]
    [Arguments("FL FR")]
    public async Task ParsePositions_AcceptsPipeWiresNotations(string value)
    {
        await Assert.That(SpaAudioChannels.ParsePositions(value)).IsEquivalentTo((string[])["FL", "FR"]);
    }
}
