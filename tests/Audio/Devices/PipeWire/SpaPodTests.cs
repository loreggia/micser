using Micser.Audio.Devices.PipeWire;

namespace Micser.Audio.Tests.Devices.PipeWire;

public class SpaPodTests
{
    // built by SPA 1.0.5's spa_format_audio_raw_build (F32, 48 kHz, FL FR FC LFE SL SR)
    private const string Format51 =
        "b00000000f000000030004000300000001000000000000000400000003000000"
        + "010000000000000002000000000000000400000003000000010000000000000001000100000000000400000003000000"
        + "1b010000000000000300010000000000040000000400000080bb00000000000004000100000000000400000004000000"
        + "06000000000000000500010000000000200000000d00000004000000030000000300000004000000050000000600000007000000"
        + "08000000";

    // the same for F32, 44.1 kHz, MONO
    private const string FormatMono =
        "a00000000f000000030004000300000001000000000000000400000003000000"
        + "010000000000000002000000000000000400000003000000010000000000000001000100000000000400000003000000"
        + "1b0100000000000003000100000000000400000004000000"
        + "44ac00000000000004000100000000000400000004000000010000000000000005000100000000000c0000000d000000"
        + "04000000030000000200000000000000";

    // a Props object built with spa_pod_builder: volume 1.0, mute true, channelVolumes [0.042844, 0.5], softMute false
    private const string Props =
        "700000000f0000000200040002000000030001000000000004000000060000000000803f00000000"
        + "0400010000000000040000000200000001000000000000000800010000000000100000000d000000"
        + "0400000006000000317d2f3d0000003f0f0001000000000004000000020000000000000000000000";

    [Test]
    public async Task AudioFormat_MatchesSpasBuilder()
    {
        var pod = SpaPod.AudioFormat(3, 48000, [3, 4, 5, 6, 7, 8]);

        await Assert.That(Convert.ToHexStringLower(pod)).IsEqualTo(Format51);
    }

    [Test]
    public async Task AudioFormat_PadsTheArrayTo8Bytes()
    {
        var pod = SpaPod.AudioFormat(3, 44100, [SpaAudioChannels.Mono]);

        await Assert.That(Convert.ToHexStringLower(pod)).IsEqualTo(FormatMono);
        await Assert.That(SpaPod.GetSize(pod)).IsEqualTo(pod.Length);
    }

    [Test]
    public async Task ReadVolume_ReadsChannelVolumesAndMute()
    {
        var (volumes, mute) = SpaPod.ReadVolume(Convert.FromHexString(Props));

        await Assert.That(volumes).IsEquivalentTo((float[])[0.042844f, 0.5f]);
        await Assert.That(mute).IsTrue();
    }

    [Test]
    public async Task ReadVolume_IgnoresOtherObjects()
    {
        var (volumes, mute) = SpaPod.ReadVolume(Convert.FromHexString(Format51));

        await Assert.That(volumes).IsNull();
        await Assert.That(mute).IsNull();
    }

    [Test]
    public async Task ReadVolume_StopsAtATruncatedProperty()
    {
        var truncated = Convert.FromHexString(Props)[..60];

        var (volumes, mute) = SpaPod.ReadVolume(truncated);

        await Assert.That(volumes).IsNull();
        await Assert.That(mute).IsTrue();
    }
}
