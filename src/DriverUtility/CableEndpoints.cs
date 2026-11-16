using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Micser.DriverUtility;

/// <summary>
/// The Windows audio endpoints of the cables. Windows keeps an endpoint's device format when the driver creates a cable's device again,
/// so after a cable's layout changed, its endpoints still have the old format, which the driver no longer offers, and can't be opened
/// until the format is set again.
/// </summary>
internal static class CableEndpoints
{
    /// <summary>
    /// The KS filter an endpoint belongs to, e.g. <c>{2}.\\?\micsercable#cable01#...#{6994ad04-...}\render</c>. Observed, not
    /// documented.
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
    /// The active endpoints of the cables.
    /// </summary>
    public static List<CableEndpoint> Find()
    {
        var enumerator = CoreAudio.CreateDeviceEnumerator();
        var policy = CoreAudio.CreatePolicyConfig();
        Marshal.ThrowExceptionForHR(
            enumerator.EnumAudioEndpoints(CoreAudio.DataFlowAll, CoreAudio.DeviceStateActive, out var devices)
        );
        Marshal.ThrowExceptionForHR(devices.GetCount(out var count));

        var endpoints = new List<CableEndpoint>();
        for (uint i = 0; i < count; i++)
        {
            if (
                devices.Item(i, out var device) != 0
                || device.OpenPropertyStore(CoreAudio.StorageRead, out var properties) != 0
            )
            {
                continue;
            }

            if (
                ReadString(properties, FilterKey) is not { } filter
                || !TryParseFilterPath(filter, out var cable, out var isCapture)
            )
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
    /// cable; <paramref name="includes"/> limits the check to some cables.
    /// </summary>
    public static bool AllMatch(
        IReadOnlyList<CableEndpoint> endpoints,
        IReadOnlyList<CableLayout> layouts,
        Func<int, bool>? includes = null
    )
    {
        return Enumerable
            .Range(1, layouts.Count)
            .Where(cable => includes?.Invoke(cable) ?? true)
            .All(cable => CableMatches(endpoints, cable, layouts[cable - 1]));
    }

    public static bool CableMatches(IReadOnlyList<CableEndpoint> endpoints, int cable, CableLayout layout)
    {
        var sides = endpoints.Where(e => e.Cable == cable).ToList();
        return sides.Any(e => !e.IsCapture) && sides.Any(e => e.IsCapture) && sides.All(e => e.Matches(layout));
    }

    /// <summary>
    /// Sets the device format of every cable endpoint whose format doesn't match its cable's layout. Endpoints appear a moment after the
    /// cable's device was created, so this retries until all match or the timeout passes. Returns whether all match.
    /// <paramref name="includes"/> limits it to some cables.
    /// </summary>
    public static bool Sync(IReadOnlyList<CableLayout> layouts, TimeSpan timeout, Func<int, bool>? includes = null)
    {
        var policy = CoreAudio.CreatePolicyConfig();
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            var endpoints = Find().Where(e => e.Cable <= layouts.Count && (includes?.Invoke(e.Cable) ?? true)).ToList();
            if (AllMatch(endpoints, layouts, includes))
            {
                return true;
            }

            foreach (var endpoint in endpoints.Where(e => !e.Matches(layouts[e.Cable - 1])))
            {
                var layout = layouts[endpoint.Cable - 1];
                var result = SetDeviceFormat(policy, endpoint.Id, layout);
                if (result == 0)
                {
                    Log.Info(
                        $"Set the format of {endpoint.Name} to {layout.GetName()} (was {endpoint.Channels?.ToString() ?? "unknown"} channels)"
                    );
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
    /// Reads the cable number and side from an endpoint's filter path, if the filter belongs to a cable device. The cable devices are
    /// MicserCable\CableNN, and their circuits' reference strings are Render and Capture (src/CableDriver/circuits.cpp).
    /// </summary>
    public static bool TryParseFilterPath(string value, out int cable, out bool isCapture)
    {
        cable = 0;
        isCapture = false;

        // "{2}.\\?\micsercable#cable01#1&14041d6&6&01#{interface class}\render"
        const string Device = @"\\?\micsercable#cable";
        var start = value.IndexOf(Device, StringComparison.OrdinalIgnoreCase);
        var number = start + Device.Length;
        if (
            start < 0
            || value.Length < number + 3
            || value[number + 2] != '#'
            || !int.TryParse(value.AsSpan(number, 2), out var parsed)
            || parsed < 1
        )
        {
            return false;
        }

        var reference = value[(value.LastIndexOf('\\') + 1)..];
        if (reference.Equals("capture", StringComparison.OrdinalIgnoreCase))
        {
            isCapture = true;
        }
        else if (!reference.Equals("render", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        cable = parsed;
        return true;
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
