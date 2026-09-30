using NAudio.Wave;

namespace Micser.Audio.Tests;

public class ChannelMixerTests
{
    private const float MinusThreeDb = 0.70710677f;

    [Test]
    public async Task BackToSide_MapsDirectly()
    {
        var source = new ChannelLayout(4, Speakers.Quad);
        var mixer = new ChannelMixer(source, ChannelLayout.Surround51);

        await Assert.That(mixer.GetGain(source.IndexOf(Speakers.BackLeft), ChannelLayout.Surround51.IndexOf(Speakers.SideLeft))).IsEqualTo(1f);
    }

    [Test]
    public async Task MixInto_AddsToTarget()
    {
        var mixer = new ChannelMixer(ChannelLayout.Mono, ChannelLayout.Stereo);
        var source = new AudioBuffer(4, ChannelLayout.Mono);
        source.GetChannel(0).Fill(0.25f);
        var target = new AudioBuffer(4, ChannelLayout.Stereo);
        target.GetChannel(0).Fill(0.5f);
        target.GetChannel(1).Clear();

        mixer.MixInto(source, target);

        await Assert.That(target.GetChannel(0)[3]).IsEqualTo(0.75f);
        await Assert.That(target.GetChannel(1)[3]).IsEqualTo(0.25f);
    }

    [Test]
    public async Task MonoToStereo_DuplicatesAtUnity()
    {
        var mixer = new ChannelMixer(ChannelLayout.Mono, ChannelLayout.Stereo);

        await Assert.That(mixer.GetGain(0, 0)).IsEqualTo(1f);
        await Assert.That(mixer.GetGain(0, 1)).IsEqualTo(1f);
    }

    [Test]
    public async Task MonoToSurround_GoesToCenter()
    {
        var mixer = new ChannelMixer(ChannelLayout.Mono, ChannelLayout.Surround51);

        await Assert.That(mixer.GetGain(0, ChannelLayout.Surround51.IndexOf(Speakers.FrontCenter))).IsEqualTo(1f);
        await Assert.That(mixer.GetGain(0, 0)).IsEqualTo(0f);
    }

    [Test]
    public async Task SameLayout_IsIdentity()
    {
        var mixer = new ChannelMixer(ChannelLayout.Stereo, ChannelLayout.Stereo);

        await Assert.That(mixer.GetGain(0, 0)).IsEqualTo(1f);
        await Assert.That(mixer.GetGain(1, 1)).IsEqualTo(1f);
        await Assert.That(mixer.GetGain(0, 1)).IsEqualTo(0f);
    }

    [Test]
    public async Task StereoToMono_Averages()
    {
        var mixer = new ChannelMixer(ChannelLayout.Stereo, ChannelLayout.Mono);

        await Assert.That(mixer.GetGain(0, 0)).IsEqualTo(0.5f);
        await Assert.That(mixer.GetGain(1, 0)).IsEqualTo(0.5f);
    }

    [Test]
    public async Task StereoToSurround_OnlyFillsFrontLeftAndRight()
    {
        var mixer = new ChannelMixer(ChannelLayout.Stereo, ChannelLayout.Surround51);

        await Assert.That(mixer.GetGain(0, 0)).IsEqualTo(1f);
        await Assert.That(mixer.GetGain(1, 1)).IsEqualTo(1f);
        for (var target = 2; target < 6; target++)
        {
            await Assert.That(mixer.GetGain(0, target) + mixer.GetGain(1, target)).IsEqualTo(0f);
        }
    }

    [Test]
    public async Task SurroundToMono_IgnoresLfe()
    {
        var mixer = new ChannelMixer(ChannelLayout.Surround51, ChannelLayout.Mono);

        await Assert.That(mixer.GetGain(ChannelLayout.Surround51.IndexOf(Speakers.LowFrequency), 0)).IsEqualTo(0f);
        await Assert.That(mixer.GetGain(0, 0)).IsEqualTo(0.2f);
    }

    [Test]
    public async Task SurroundToStereo_FoldsCenterAndSidesAtMinusThreeDb()
    {
        var source = ChannelLayout.Surround51;
        var mixer = new ChannelMixer(source, ChannelLayout.Stereo);

        await Assert.That(mixer.GetGain(source.IndexOf(Speakers.FrontLeft), 0)).IsEqualTo(1f);
        await Assert.That(mixer.GetGain(source.IndexOf(Speakers.FrontCenter), 0)).IsEqualTo(MinusThreeDb);
        await Assert.That(mixer.GetGain(source.IndexOf(Speakers.FrontCenter), 1)).IsEqualTo(MinusThreeDb);
        await Assert.That(mixer.GetGain(source.IndexOf(Speakers.SideLeft), 0)).IsEqualTo(MinusThreeDb);
        await Assert.That(mixer.GetGain(source.IndexOf(Speakers.SideLeft), 1)).IsEqualTo(0f);
        await Assert.That(mixer.GetGain(source.IndexOf(Speakers.LowFrequency), 0)).IsEqualTo(0f);
    }

    [Test]
    public async Task WithoutSpeakerPositions_MapsByIndex()
    {
        var mixer = new ChannelMixer(ChannelLayout.FromChannelCount(3), ChannelLayout.Stereo);

        await Assert.That(mixer.GetGain(0, 0)).IsEqualTo(1f);
        await Assert.That(mixer.GetGain(1, 1)).IsEqualTo(1f);
        await Assert.That(mixer.GetGain(2, 0) + mixer.GetGain(2, 1)).IsEqualTo(0f);
    }
}
