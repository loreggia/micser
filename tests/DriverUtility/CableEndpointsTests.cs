using System.Buffers.Binary;

namespace Micser.DriverUtility.Tests;

public class CableEndpointsTests
{
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
        await Assert
            .That(CableEndpoints.AllMatch(endpoints, [CableLayout.Surround71, CableLayout.Stereo], cable => cable != 2))
            .IsTrue();
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
    [Arguments(@"{2}.\\?\micsercable#cable01#1&14041d6&6&01#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\render", 1, false)]
    [Arguments(@"{2}.\\?\MICSERCABLE#CABLE12#1&14041d6&6&12#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\Capture", 12, true)]
    public async Task TryParseFilterPath_ReadsCableAndSide(string path, int cable, bool isCapture)
    {
        var parsed = CableEndpoints.TryParseFilterPath(path, out var parsedCable, out var parsedCapture);

        await Assert.That(parsed).IsTrue();
        await Assert.That(parsedCable).IsEqualTo(cable);
        await Assert.That(parsedCapture).IsEqualTo(isCapture);
    }

    [Test]
    [Arguments(@"{2}.\\?\micsercable#cable01#1&14041d6&6&01#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\topology")]
    [Arguments(@"{2}.\\?\micsercable#cable00#1&14041d6&6&00#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\render")]
    [Arguments(@"{2}.\\?\micsercable#cable1#1&14041d6&6&01#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\render")]
    [Arguments(@"{2}.\\?\other#cable01#1&14041d6&6&01#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\render")]
    [Arguments(@"{2}.\\?\hdaudio#func_01#{6994ad04-93ef-11d0-a3cc-00a0c9223196}\elineoutwave")]
    public async Task TryParseFilterPath_OtherFilters_AreIgnored(string path)
    {
        await Assert.That(CableEndpoints.TryParseFilterPath(path, out _, out _)).IsFalse();
    }
}

public class CableConfigTests
{
    [Test]
    public async Task NativeLayout_MatchesPublicH()
    {
        // MICSERCABLE_CONFIG: CableCount, Channels[16], ActiveCables
        await Assert.That(System.Runtime.CompilerServices.Unsafe.SizeOf<MICSERCABLE_CONFIG>()).IsEqualTo(72);
        await Assert
            .That(System.Runtime.InteropServices.Marshal.OffsetOf<MICSERCABLE_CONFIG>("ActiveCables"))
            .IsEqualTo(68);
    }

    [Test]
    public async Task ToNative_AndBack_KeepsTheConfig()
    {
        CableLayout[] layouts =
        [
            CableLayout.Stereo,
            CableLayout.Surround51,
            .. Enumerable.Repeat(CableLayout.Surround71, 14),
        ];
        var config = new CableConfig(3, layouts);

        var native = config.ToNative();
        native.ActiveCables = 0b101;
        var back = CableConfig.FromNative(native);

        await Assert.That(back.CableCount).IsEqualTo(3);
        await Assert.That(back.Layouts).IsEquivalentTo(layouts);
        await Assert
            .That(back.ActiveLayouts)
            .IsEquivalentTo([CableLayout.Stereo, CableLayout.Surround51, CableLayout.Surround71]);
        await Assert.That(back.ActiveCables).IsEqualTo(0b101u);
        await Assert.That(back.Exists(1)).IsTrue();
        await Assert.That(back.Exists(2)).IsFalse();
        await Assert.That(back.Exists(3)).IsTrue();
    }

    [Test]
    public async Task FromNative_UnsupportedChannels_AreStereo()
    {
        var native = new CableConfig(1, [(CableLayout)4]).ToNative();

        await Assert.That(CableConfig.FromNative(native).Layouts[0]).IsEqualTo(CableLayout.Stereo);
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
        await Assert
            .That(CableLayouts.TryParse("7.1", out var surround) && surround == CableLayout.Surround71)
            .IsTrue();
        await Assert.That(CableLayouts.TryParse("Stereo", out var stereo) && stereo == CableLayout.Stereo).IsTrue();
        await Assert.That(CableLayouts.TryParse("quad", out _)).IsFalse();
    }
}
