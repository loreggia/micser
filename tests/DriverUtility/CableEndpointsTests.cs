using System.Buffers.Binary;

namespace Micser.DriverUtility.Tests;

public class CableEndpointsTests
{
    private const string InstanceId = @"ROOT\MEDIA\0000";

    [Test]
    public async Task CableMatches_NeedsBothSidesWithTheLayoutsFormat()
    {
        CableEndpoint[] endpoints =
        [
            new("render1", 1, false, 8, 0x63F),
            new("capture1", 1, true, 8, 0x63F),
            new("render2", 2, false, 2, 0x3),
            new("capture2", 2, true, 8, 0x63F),
            new("render3", 3, false, 2, 0x3),
        ];

        await Assert.That(CableEndpoints.CableMatches(endpoints, 1, CableLayout.Surround71)).IsTrue();
        await Assert.That(CableEndpoints.CableMatches(endpoints, 1, CableLayout.Stereo)).IsFalse();
        await Assert.That(CableEndpoints.CableMatches(endpoints, 2, CableLayout.Stereo)).IsFalse();
        await Assert.That(CableEndpoints.CableMatches(endpoints, 3, CableLayout.Stereo)).IsFalse();
        await Assert.That(CableEndpoints.AllMatch(endpoints, [CableLayout.Surround71])).IsTrue();
        await Assert.That(CableEndpoints.AllMatch(endpoints, [CableLayout.Surround71, CableLayout.Stereo])).IsFalse();
    }

    [Test]
    public async Task CreateFormat_DescribesTheLayout()
    {
        var device = CableEndpoints.CreateFormat(CableLayout.Surround51, false);
        var mix = CableEndpoints.CreateFormat(CableLayout.Surround51, true);

        await Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(device.AsSpan(0))).IsEqualTo((ushort)0xFFFE);
        await Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(device.AsSpan(2))).IsEqualTo((ushort)6);
        await Assert.That(BinaryPrimitives.ReadUInt32LittleEndian(device.AsSpan(4))).IsEqualTo(48000u);
        await Assert.That(BinaryPrimitives.ReadUInt32LittleEndian(device.AsSpan(8))).IsEqualTo(48000u * 24);
        await Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(device.AsSpan(12))).IsEqualTo((ushort)24);
        await Assert.That(BinaryPrimitives.ReadUInt32LittleEndian(device.AsSpan(20))).IsEqualTo(0x60Fu);
        await Assert.That(new Guid(device.AsSpan(24, 16))).IsEqualTo(new Guid("00000001-0000-0010-8000-00aa00389b71"));
        await Assert.That(new Guid(mix.AsSpan(24, 16))).IsEqualTo(new Guid("00000003-0000-0010-8000-00aa00389b71"));
    }

    [Test]
    [Arguments(@"{2}.\\?\root#media#0000#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\waverender1", 1, false)]
    [Arguments(@"{2}.\\?\root#media#0000#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\wavecapture12", 12, true)]
    public async Task TryParseFilterPath_ReadsCableAndSide(string path, int cable, bool isCapture)
    {
        var parsed = CableEndpoints.TryParseFilterPath(path, InstanceId, out var parsedCable, out var parsedCapture);

        await Assert.That(parsed).IsTrue();
        await Assert.That(parsedCable).IsEqualTo(cable);
        await Assert.That(parsedCapture).IsEqualTo(isCapture);
    }

    [Test]
    [Arguments(@"{2}.\\?\root#media#0001#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\waverender1")]
    [Arguments(@"{2}.\\?\root#media#0000#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\topologyrender1")]
    [Arguments(@"{2}.\\?\root#media#0000#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\waverender")]
    [Arguments(@"{2}.\\?\hdaudio#func_01#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\elineoutwave")]
    public async Task TryParseFilterPath_OtherFilters_AreIgnored(string path)
    {
        await Assert.That(CableEndpoints.TryParseFilterPath(path, InstanceId, out _, out _)).IsFalse();
    }
}

public class CableLayoutsTests
{
    [Test]
    public async Task FromChannels_UnsupportedValues_AreStereo()
    {
        await Assert.That(CableLayouts.FromChannels(8)).IsEqualTo(CableLayout.Surround71);
        await Assert.That(CableLayouts.FromChannels(6)).IsEqualTo(CableLayout.Surround51);
        await Assert.That(CableLayouts.FromChannels(4)).IsEqualTo(CableLayout.Stereo);
        await Assert.That(CableLayouts.FromChannels(null)).IsEqualTo(CableLayout.Stereo);
        await Assert.That(CableLayouts.FromChannels("8")).IsEqualTo(CableLayout.Stereo);
    }

    [Test]
    public async Task TryParse_ReadsTheNames()
    {
        await Assert.That(CableLayouts.TryParse("7.1", out var surround) && surround == CableLayout.Surround71).IsTrue();
        await Assert.That(CableLayouts.TryParse("Stereo", out var stereo) && stereo == CableLayout.Stereo).IsTrue();
        await Assert.That(CableLayouts.TryParse("quad", out _)).IsFalse();
    }
}
