using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using static Micser.DriverUtility.NativeMethods;

namespace Micser.DriverUtility;

/// <summary>
/// The root-enumerated device of the cable driver. Owns the device information set it was found in or created with. The driver creates
/// a child device per cable (MicserCable\CableNN), and Windows an endpoint device per side of each cable.
/// </summary>
internal sealed unsafe class CableDriverDevice : IDisposable
{
    public const string CableCountValue = "CableCount";
    public const string CableLayoutValueFormat = "Cable{0}Channels";
    public const string HardwareId = @"ROOT\MicserCable";
    public const int MaxCableCount = 16;

    /// <summary>
    /// The enumerator of the cable devices, which the driver creates (MicserCable\Cable01, ...).
    /// </summary>
    private const string CableEnumerator = "MicserCable";

    private readonly nint _deviceInfoSet;
    private SP_DEVINFO_DATA _deviceInfoData;

    private CableDriverDevice(nint deviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
    {
        _deviceInfoSet = deviceInfoSet;
        _deviceInfoData = deviceInfoData;
    }

    /// <summary>
    /// Whether the device is present (not only a leftover devnode).
    /// </summary>
    public bool IsPresent => CM_Get_DevNode_Status(out _, out _, _deviceInfoData.DevInst, 0) == CR_SUCCESS;

    /// <summary>
    /// The device's problem code, or null when it runs.
    /// </summary>
    public uint? Problem
    {
        get
        {
            if (CM_Get_DevNode_Status(out var status, out var problem, _deviceInfoData.DevInst, 0) != CR_SUCCESS)
            {
                return null;
            }

            return (status & DN_HAS_PROBLEM) != 0 ? problem : null;
        }
    }

    /// <summary>
    /// Creates the devnode for an INF's device class. The driver is installed separately (<see cref="DriverInstaller"/>).
    /// </summary>
    public static CableDriverDevice Create(string infPath, int cableCount)
    {
        var className = stackalloc char[64];
        if (!SetupDiGetINFClass(infPath, out var classGuid, className, 64, out _))
        {
            throw LastError(nameof(SetupDiGetINFClass));
        }

        var deviceInfoSet = SetupDiCreateDeviceInfoList(ref classGuid, 0);
        if (deviceInfoSet == InvalidHandle)
        {
            throw LastError(nameof(SetupDiCreateDeviceInfoList));
        }

        var device = new CableDriverDevice(
            deviceInfoSet,
            new SP_DEVINFO_DATA { cbSize = (uint)sizeof(SP_DEVINFO_DATA) }
        );
        try
        {
            if (
                !SetupDiCreateDeviceInfo(
                    deviceInfoSet,
                    new string(className),
                    ref classGuid,
                    null,
                    0,
                    DICD_GENERATE_ID,
                    ref device._deviceInfoData
                )
            )
            {
                throw LastError(nameof(SetupDiCreateDeviceInfo));
            }

            // REG_MULTI_SZ
            var hardwareIds = Encoding.Unicode.GetBytes(HardwareId + "\0\0");
            fixed (byte* buffer = hardwareIds)
            {
                if (
                    !SetupDiSetDeviceRegistryProperty(
                        deviceInfoSet,
                        ref device._deviceInfoData,
                        SPDRP_HARDWAREID,
                        buffer,
                        (uint)hardwareIds.Length
                    )
                )
                {
                    throw LastError(nameof(SetupDiSetDeviceRegistryProperty));
                }
            }

            if (!SetupDiCallClassInstaller(DIF_REGISTERDEVICE, deviceInfoSet, ref device._deviceInfoData))
            {
                throw LastError("SetupDiCallClassInstaller(DIF_REGISTERDEVICE)");
            }

            // before the driver starts; the INF keeps an existing value
            var key = SetupDiCreateDevRegKey(
                deviceInfoSet,
                ref device._deviceInfoData,
                DICS_FLAG_GLOBAL,
                0,
                DIREG_DEV,
                0,
                null
            );
            if (key == InvalidHandle)
            {
                throw LastError(nameof(SetupDiCreateDevRegKey));
            }

            using var handle = new SafeRegistryHandle(key, true);
            using var registryKey = RegistryKey.FromHandle(handle);
            registryKey.SetValue(CableCountValue, Math.Clamp(cableCount, 1, MaxCableCount), RegistryValueKind.DWord);
            return device;
        }
        catch
        {
            device.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Finds the device, preferring a present one over a leftover devnode. Null when there is none.
    /// </summary>
    public static CableDriverDevice? Find()
    {
        CableDriverDevice? found = null;
        ForEach(
            "ROOT",
            device => device.HasHardwareId(),
            device =>
            {
                if (found == null || (!found.IsPresent && device.IsPresent))
                {
                    found = device;
                }
            },
            out var deviceInfoSet
        );

        if (found == null)
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
            return null;
        }

        return new CableDriverDevice(deviceInfoSet, found._deviceInfoData);
    }

    /// <summary>
    /// Removes every devnode of the driver, including leftovers: the cables' endpoints, the cable devices and the root device. Without
    /// that, a new installation would create new cable devices next to the old ones, and Windows would number the endpoint names
    /// ("2- Micser Cable 1"). Returns whether a reboot is needed to finish.
    /// </summary>
    public static bool RemoveAll()
    {
        var needReboot = false;
        void Uninstall(CableDriverDevice device)
        {
            if (!DiUninstallDevice(0, device._deviceInfoSet, ref device._deviceInfoData, 0, out var reboot))
            {
                throw LastError(nameof(DiUninstallDevice));
            }

            needReboot |= reboot;
        }

        // the endpoints are software devices whose parent is a cable device
        ForEach(
            "SWD",
            device =>
                device
                    .GetStringProperty(ParentKey)
                    ?.StartsWith(CableEnumerator + @"\", StringComparison.OrdinalIgnoreCase) == true,
            Uninstall,
            out var deviceInfoSet
        );
        SetupDiDestroyDeviceInfoList(deviceInfoSet);

        ForEach(CableEnumerator, _ => true, Uninstall, out deviceInfoSet);
        SetupDiDestroyDeviceInfoList(deviceInfoSet);

        ForEach("ROOT", device => device.HasHardwareId(), Uninstall, out deviceInfoSet);
        SetupDiDestroyDeviceInfoList(deviceInfoSet);
        return needReboot;
    }

    public void Dispose()
    {
        SetupDiDestroyDeviceInfoList(_deviceInfoSet);
    }

    public int GetCableCount()
    {
        return ReadValue(CableCountValue) is int count ? Math.Clamp(count, 1, MaxCableCount) : 1;
    }

    /// <summary>
    /// The layout of each cable (index 0 is cable 1), as the driver keeps it.
    /// </summary>
    public IReadOnlyList<CableLayout> GetCableLayouts()
    {
        return
        [
            .. Enumerable
                .Range(1, GetCableCount())
                .Select(cable => CableLayouts.FromChannels(ReadValue(GetCableLayoutValue(cable)))),
        ];
    }

    /// <summary>
    /// The published name of the installed INF (e.g. oem7.inf), or null without a driver.
    /// </summary>
    public string? GetDriverInfName()
    {
        return GetStringProperty(DriverInfPathKey);
    }

    public string? GetDriverVersion()
    {
        return GetStringProperty(DriverVersionKey);
    }

    private static string GetCableLayoutValue(int cable)
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, CableLayoutValueFormat, cable);
    }

    /// <summary>
    /// Calls <paramref name="action"/> for each devnode of an enumerator, present or not, that matches <paramref name="filter"/>.
    /// </summary>
    private static void ForEach(
        string enumerator,
        Func<CableDriverDevice, bool> filter,
        Action<CableDriverDevice> action,
        out nint deviceInfoSet
    )
    {
        deviceInfoSet = SetupDiGetClassDevs(0, enumerator, 0, DIGCF_ALLCLASSES);
        if (deviceInfoSet == InvalidHandle)
        {
            // an enumerator without devices
            if (Marshal.GetLastPInvokeError() == ERROR_INVALID_DATA)
            {
                deviceInfoSet = SetupDiCreateDeviceInfoList(0, 0);
                return;
            }

            throw LastError(nameof(SetupDiGetClassDevs));
        }

        var data = new SP_DEVINFO_DATA { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };
        for (uint i = 0; SetupDiEnumDeviceInfo(deviceInfoSet, i, ref data); i++)
        {
            var device = new CableDriverDevice(deviceInfoSet, data);
            if (filter(device))
            {
                action(device);
            }
        }
    }

    private static Win32Exception LastError(string function)
    {
        var error = Marshal.GetLastPInvokeError();
        return new Win32Exception(error, $"{function} failed: {new Win32Exception(error).Message}");
    }

    private object? ReadValue(string name)
    {
        var key = SetupDiOpenDevRegKey(_deviceInfoSet, ref _deviceInfoData, DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_READ);
        if (key == InvalidHandle)
        {
            return null;
        }

        using var handle = new SafeRegistryHandle(key, true);
        using var registryKey = RegistryKey.FromHandle(handle);
        return registryKey.GetValue(name);
    }

    private string? GetStringProperty(in DEVPROPKEY propertyKey)
    {
        SetupDiGetDeviceProperty(_deviceInfoSet, ref _deviceInfoData, propertyKey, out _, null, 0, out var size, 0);
        if (size == 0)
        {
            return null;
        }

        var buffer = new byte[size];
        fixed (byte* pointer = buffer)
        {
            if (
                !SetupDiGetDeviceProperty(
                    _deviceInfoSet,
                    ref _deviceInfoData,
                    propertyKey,
                    out _,
                    pointer,
                    size,
                    out _,
                    0
                )
            )
            {
                return null;
            }
        }

        return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }

    private bool HasHardwareId()
    {
        SetupDiGetDeviceRegistryProperty(
            _deviceInfoSet,
            ref _deviceInfoData,
            SPDRP_HARDWAREID,
            out _,
            null,
            0,
            out var size
        );
        if (size == 0)
        {
            return false;
        }

        var buffer = new byte[size];
        fixed (byte* pointer = buffer)
        {
            if (
                !SetupDiGetDeviceRegistryProperty(
                    _deviceInfoSet,
                    ref _deviceInfoData,
                    SPDRP_HARDWAREID,
                    out _,
                    pointer,
                    size,
                    out _
                )
            )
            {
                return false;
            }
        }

        return Encoding.Unicode.GetString(buffer).Split('\0').Contains(HardwareId, StringComparer.OrdinalIgnoreCase);
    }
}
