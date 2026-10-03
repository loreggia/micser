using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Micser.DriverUtility;

/// <summary>
/// The Windows audio endpoints of the cables. Windows keeps an endpoint's device format across device restarts, so after a cable's layout
/// changed, its endpoints still have the old format, which the driver no longer offers, and can't be opened until the format is set again.
/// </summary>
internal static class CableEndpoints
{
    /// <summary>
    /// The KS filter an endpoint belongs to, e.g. <c>{2}.\\?\root#media#0000#{6994ad04-...}\waverender1</c>. Observed, not documented.
    /// </summary>
    private static readonly PROPERTYKEY FilterKey = new(new Guid("233164c8-1b2c-4c7d-bc68-b671687a2567"), 1);

    private static readonly Guid IeeeFloatSubFormat = new("00000003-0000-0010-8000-00aa00389b71");
    private static readonly Guid PcmSubFormat = new("00000001-0000-0010-8000-00aa00389b71");

    /// <summary>
    /// The 48 kHz 32-bit WAVEFORMATEXTENSIBLE of a layout: integer PCM as the device format, float as the mix format.
    /// </summary>
    public static byte[] CreateFormat(CableLayout layout, bool isFloat)
    {
        var channels = (int)layout;
        var format = new byte[40];
        BinaryPrimitives.WriteUInt16LittleEndian(format.AsSpan(0), 0xFFFE);
        BinaryPrimitives.WriteUInt16LittleEndian(format.AsSpan(2), (ushort)channels);
        BinaryPrimitives.WriteUInt32LittleEndian(format.AsSpan(4), 48000);
        BinaryPrimitives.WriteUInt32LittleEndian(format.AsSpan(8), (uint)(48000 * channels * 4));
        BinaryPrimitives.WriteUInt16LittleEndian(format.AsSpan(12), (ushort)(channels * 4));
        BinaryPrimitives.WriteUInt16LittleEndian(format.AsSpan(14), 32);
        BinaryPrimitives.WriteUInt16LittleEndian(format.AsSpan(16), 22);
        BinaryPrimitives.WriteUInt16LittleEndian(format.AsSpan(18), 32);
        BinaryPrimitives.WriteUInt32LittleEndian(format.AsSpan(20), layout.GetChannelMask());
        (isFloat ? IeeeFloatSubFormat : PcmSubFormat).TryWriteBytes(format.AsSpan(24));
        return format;
    }

    /// <summary>
    /// The active endpoints of the cables of the device with the given instance ID (e.g. <c>ROOT\MEDIA\0000</c>).
    /// </summary>
    public static List<CableEndpoint> Find(string deviceInstanceId)
    {
        var enumerator = CoreAudio.CreateDeviceEnumerator();
        var policy = CoreAudio.CreatePolicyConfig();
        Marshal.ThrowExceptionForHR(enumerator.EnumAudioEndpoints(CoreAudio.DataFlowAll, CoreAudio.DeviceStateActive, out var devices));
        Marshal.ThrowExceptionForHR(devices.GetCount(out var count));

        var endpoints = new List<CableEndpoint>();
        for (uint i = 0; i < count; i++)
        {
            if (devices.Item(i, out var device) != 0 || device.OpenPropertyStore(CoreAudio.StorageRead, out var properties) != 0)
            {
                continue;
            }

            if (ReadString(properties, FilterKey) is not { } filter || !TryParseFilterPath(filter, deviceInstanceId, out var cable, out var isCapture))
            {
                continue;
            }

            Marshal.ThrowExceptionForHR(device.GetId(out var idPointer));
            var id = Marshal.PtrToStringUni(idPointer)!;
            CoreAudio.CoTaskMemFree(idPointer);

            var (channels, mask) = ReadDeviceFormat(policy, id);
            endpoints.Add(new CableEndpoint(id, cable, isCapture, channels, mask));
        }

        return endpoints;
    }

    /// <summary>
    /// Whether the endpoints of every cable exist and have the format of the cable's layout. <paramref name="layouts"/> has one entry per
    /// cable.
    /// </summary>
    public static bool AllMatch(IReadOnlyList<CableEndpoint> endpoints, IReadOnlyList<CableLayout> layouts)
    {
        return Enumerable.Range(1, layouts.Count).All(cable => CableMatches(endpoints, cable, layouts[cable - 1]));
    }

    public static bool CableMatches(IReadOnlyList<CableEndpoint> endpoints, int cable, CableLayout layout)
    {
        var sides = endpoints.Where(e => e.Cable == cable).ToList();
        return sides.Any(e => !e.IsCapture) && sides.Any(e => e.IsCapture) && sides.All(e => e.Matches(layout));
    }

    /// <summary>
    /// Sets the device format of every cable endpoint whose format doesn't match its cable's layout. Endpoints appear a moment after the
    /// device (re)started, so this retries until all match or the timeout passes. Returns whether all match.
    /// </summary>
    public static bool Sync(string deviceInstanceId, IReadOnlyList<CableLayout> layouts, TimeSpan timeout)
    {
        var policy = CoreAudio.CreatePolicyConfig();
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            var endpoints = Find(deviceInstanceId).Where(e => e.Cable <= layouts.Count).ToList();
            if (AllMatch(endpoints, layouts))
            {
                return true;
            }

            foreach (var endpoint in endpoints.Where(e => !e.Matches(layouts[e.Cable - 1])))
            {
                var layout = layouts[endpoint.Cable - 1];
                var result = SetDeviceFormat(policy, endpoint.Id, layout);
                if (result == 0)
                {
                    Log.Info($"Set the format of {endpoint.Name} to {layout.GetName()} (was {endpoint.Channels?.ToString() ?? "unknown"} channels)");
                }
                else
                {
                    Log.Warning($"Setting the format of {endpoint.Name} to {layout.GetName()} failed: 0x{result:X8}");
                }
            }

            if (stopwatch.Elapsed > timeout)
            {
                Log.Warning("The cables' endpoints don't all have the format of their layout.");
                return false;
            }

            Thread.Sleep(500);
        }
    }

    /// <summary>
    /// Reads the cable number and side from an endpoint's filter path, if the filter belongs to the device.
    /// </summary>
    public static bool TryParseFilterPath(string value, string deviceInstanceId, out int cable, out bool isCapture)
    {
        cable = 0;
        isCapture = false;

        // "{2}.\\?\root#media#0000#{interface class}\waverender1"
        var device = @"\\?\" + deviceInstanceId.Replace('\\', '#') + "#";
        var start = value.IndexOf(device, StringComparison.OrdinalIgnoreCase);
        var separator = value.LastIndexOf('\\');
        if (start < 0 || separator < start + device.Length)
        {
            return false;
        }

        var reference = value[(separator + 1)..];
        foreach (var (prefix, capture) in new[] { ("waverender", false), ("wavecapture", true) })
        {
            if (reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && int.TryParse(reference[prefix.Length..], out cable) && cable > 0)
            {
                isCapture = capture;
                return true;
            }
        }

        cable = 0;
        return false;
    }

    private static (int? Channels, uint? Mask) ReadDeviceFormat(IPolicyConfig policy, string id)
    {
        if (policy.GetDeviceFormat(id, 0, out var format) != 0 || format == 0)
        {
            return (null, null);
        }

        try
        {
            var channels = Marshal.ReadInt16(format, 2);
            var isExtensible = (ushort)Marshal.ReadInt16(format, 0) == 0xFFFE && Marshal.ReadInt16(format, 16) >= 22;
            return (channels, isExtensible ? (uint)Marshal.ReadInt32(format, 20) : null);
        }
        finally
        {
            CoreAudio.CoTaskMemFree(format);
        }
    }

    private static string? ReadString(IPropertyStore properties, in PROPERTYKEY key)
    {
        if (properties.GetValue(key, out var value) != 0)
        {
            return null;
        }

        try
        {
            return value.VarType == PROPVARIANT.VtLpwstr ? Marshal.PtrToStringUni(value.Value) : null;
        }
        finally
        {
            CoreAudio.PropVariantClear(ref value);
        }
    }

    private static unsafe int SetDeviceFormat(IPolicyConfig policy, string id, CableLayout layout)
    {
        fixed (byte* device = CreateFormat(layout, false))
        fixed (byte* mix = CreateFormat(layout, true))
        {
            return policy.SetDeviceFormat(id, (nint)device, (nint)mix);
        }
    }
}

/// <param name="Channels">The channels of the endpoint's device format; null if Windows has none for it.</param>
/// <param name="ChannelMask">The speaker mask of the device format; null if it has none.</param>
internal sealed record CableEndpoint(string Id, int Cable, bool IsCapture, int? Channels, uint? ChannelMask)
{
    public string Name => $"cable {Cable} {(IsCapture ? "output" : "input")}";

    public bool Matches(CableLayout layout)
    {
        return Channels == (int)layout && (ChannelMask == null || ChannelMask == layout.GetChannelMask());
    }
}
