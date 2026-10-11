using System.Runtime.Versioning;
using NAudio.CoreAudioApi;

namespace Micser.Audio.Devices.Wasapi;

[SupportedOSPlatform("windows")]
internal static class WasapiDevices
{
    /// <summary>
    /// The device's default shared-mode period, in seconds.
    /// </summary>
    public static double GetDevicePeriod(MMDevice device)
    {
        using var client = device.CreateAudioClient();

        // 100 ns units
        return TimeSpan.FromTicks(client.DefaultDevicePeriod).TotalSeconds;
    }
}
