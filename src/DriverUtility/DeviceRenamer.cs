using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Serilog;

namespace Micser.DriverUtility;

/// <summary>
/// Names the virtual endpoints "Micser Virtual Audio Cable 1..n" in driver order, once the driver has created them.
/// </summary>
internal static partial class DeviceRenamer
{
    private const int Retries = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public static async Task<bool> RenameDevicesAsync(int expectedCount)
    {
        using var enumerator = new MMDeviceEnumerator();

        for (var attempt = 1; attempt <= Retries; attempt++)
        {
            var renderDevices = GetVirtualDevices(enumerator, DataFlow.Render);
            var captureDevices = GetVirtualDevices(enumerator, DataFlow.Capture);

            try
            {
                if (renderDevices.Count == expectedCount && captureDevices.Count == expectedCount)
                {
                    Rename(renderDevices);
                    Rename(captureDevices);
                    return true;
                }

                Log.Information(
                    "Expected {Expected} devices, found {Render} render and {Capture} capture devices. Retrying ({Attempt}/{Retries})...",
                    expectedCount, renderDevices.Count, captureDevices.Count, attempt, Retries);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Renaming the devices failed.");
                return false;
            }
            finally
            {
                renderDevices.ForEach(d => d.Dispose());
                captureDevices.ForEach(d => d.Dispose());
            }

            await Task.Delay(RetryDelay);
        }

        return false;
    }

    private static string? GetString(MMDevice device, PropertyKey key)
    {
        try
        {
            return device.Properties.Contains(key) ? device.Properties[key].Value as string : null;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not read a property of {Device}.", device.ID);
            return null;
        }
    }

    private static List<MMDevice> GetVirtualDevices(MMDeviceEnumerator enumerator, DataFlow dataFlow)
    {
        var result = new List<MMDevice>();
        foreach (var device in enumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active | DeviceState.Disabled))
        {
            if (GetString(device, DriverGlobals.DeviceInterfaceNameKey) == DriverGlobals.DeviceInterfaceName)
            {
                result.Add(device);
            }
            else
            {
                device.Dispose();
            }
        }

        return result;
    }

    private static int GetWaveIndex(MMDevice device)
    {
        var topology = GetString(device, DriverGlobals.TopologyInfoKey);
        var match = topology == null ? null : WaveIndexRegex().Match(topology);
        return match is { Success: true } && int.TryParse(match.Groups[1].Value, out var index) ? index : int.MaxValue;
    }

    private static void Rename(List<MMDevice> devices)
    {
        devices.Sort((d1, d2) => GetWaveIndex(d1).CompareTo(GetWaveIndex(d2)));

        for (var i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            var name = $"{DriverGlobals.DeviceInterfaceName} {i + 1}";
            var value = Marshal.StringToCoTaskMemUni(name);
            try
            {
                device.GetPropertyInformation(StorageAccessMode.ReadWrite);
                device.Properties.SetValue(DriverGlobals.DeviceDescriptionKey, new PropVariant { vt = (short)VarEnum.VT_LPWSTR, pointerValue = value });
                device.Properties.Commit();
            }
            finally
            {
                Marshal.FreeCoTaskMem(value);
            }

            Log.Information("Renamed {Device} to {Name}.", device.ID, name);
        }
    }

    [GeneratedRegex(@"\\wave(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex WaveIndexRegex();
}
