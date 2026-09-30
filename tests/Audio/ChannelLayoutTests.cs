using NAudio.Wave;

namespace Micser.Audio.Tests;

public class ChannelLayoutTests
{
    [Test]
    [Arguments(1, Speakers.Mono)]
    [Arguments(2, Speakers.Stereo)]
    [Arguments(6, Speakers.Surround51)]
    [Arguments(8, Speakers.Surround71)]
    public async Task FromChannelCount_UsesStandardSpeakers(int channelCount, Speakers speakers)
    {
        var layout = ChannelLayout.FromChannelCount(channelCount);

        await Assert.That(layout.Speakers).IsEqualTo(speakers);
        await Assert.That(layout.HasSpeakerPositions).IsTrue();
    }

    [Test]
    public async Task FromChannelCount_WithoutStandardLayout_HasNoSpeakerPositions()
    {
        var layout = ChannelLayout.FromChannelCount(3);

        await Assert.That(layout.HasSpeakerPositions).IsFalse();
        await Assert.That(layout.GetSpeaker(2)).IsEqualTo(Speakers.None);
        await Assert.That(layout.IndexOf(Speakers.FrontLeft)).IsEqualTo(-1);
    }

    [Test]
    public async Task FromWaveFormat_UsesChannelMask()
    {
        var format = new WaveFormatExtensible(48000, 32, 2, true, 32, Speakers.FrontCenter | Speakers.LowFrequency);

        var layout = ChannelLayout.FromWaveFormat(format);

        await Assert.That(layout).IsEqualTo(new ChannelLayout(2, Speakers.FrontCenter | Speakers.LowFrequency));
    }

    [Test]
    public async Task FromWaveFormat_WithMismatchingMask_FallsBackToChannelCount()
    {
        var format = new WaveFormatExtensible(48000, 32, 2, true, 32, Speakers.Mono);

        var layout = ChannelLayout.FromWaveFormat(format);

        await Assert.That(layout).IsEqualTo(ChannelLayout.Stereo);
    }

    [Test]
    public async Task GetSpeakerAndIndexOf_FollowSpeakerBitOrder()
    {
        var layout = ChannelLayout.Surround51;

        await Assert.That(layout.GetSpeaker(0)).IsEqualTo(Speakers.FrontLeft);
        await Assert.That(layout.GetSpeaker(2)).IsEqualTo(Speakers.FrontCenter);
        await Assert.That(layout.GetSpeaker(3)).IsEqualTo(Speakers.LowFrequency);
        await Assert.That(layout.GetSpeaker(5)).IsEqualTo(Speakers.SideRight);
        await Assert.That(layout.IndexOf(Speakers.SideLeft)).IsEqualTo(4);
        await Assert.That(layout.IndexOf(Speakers.BackLeft)).IsEqualTo(-1);
    }
}
