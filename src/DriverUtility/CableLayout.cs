namespace Micser.DriverUtility;

/// <summary>
/// The channel layout of a cable; the value is its channel count, which the driver reads from "Cable&lt;n&gt;Channels".
/// </summary>
internal enum CableLayout
{
    Stereo = 2,
    Surround51 = 6,
    Surround71 = 8,
}

internal static class CableLayouts
{
    /// <summary>
    /// The layout of a channel count read from the registry; stereo for anything the driver doesn't support.
    /// </summary>
    public static CableLayout FromChannels(object? channels)
    {
        return channels is int value && Enum.IsDefined((CableLayout)value) ? (CableLayout)value : CableLayout.Stereo;
    }

    /// <summary>
    /// The speaker mask of the layout, as the driver uses it: stereo, 5.1 with side speakers (Windows' usual 5.1) or 7.1.
    /// </summary>
    public static uint GetChannelMask(this CableLayout layout)
    {
        return layout switch
        {
            CableLayout.Surround51 => 0x60F,
            CableLayout.Surround71 => 0x63F,
            _ => 0x3,
        };
    }

    /// <summary>
    /// The name used on the command line and in the status: "stereo", "5.1" or "7.1".
    /// </summary>
    public static string GetName(this CableLayout layout)
    {
        return layout switch
        {
            CableLayout.Surround51 => "5.1",
            CableLayout.Surround71 => "7.1",
            _ => "stereo",
        };
    }

    public static bool TryParse(string value, out CableLayout layout)
    {
        foreach (var candidate in Enum.GetValues<CableLayout>())
        {
            if (string.Equals(candidate.GetName(), value, StringComparison.OrdinalIgnoreCase))
            {
                layout = candidate;
                return true;
            }
        }

        layout = CableLayout.Stereo;
        return false;
    }
}
