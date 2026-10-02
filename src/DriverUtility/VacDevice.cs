using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using static Micser.DriverUtility.NativeMethods;

namespace Micser.DriverUtility;

/// <summary>
/// The root-enumerated VAC device. Owns the device information set it was found in or created with.
/// </summary>
internal sealed unsafe class VacDevice : IDisposable
{
    public const string CableCountValue = "CableCount";
    public const string HardwareId = @"ROOT\MicserVac";
    public const int MaxCableCount = 16;

    private readonly nint _deviceInfoSet;
    private SP_DEVINFO_DATA _deviceInfoData;

    private VacDevice(nint deviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
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
    public static VacDevice Create(string infPath, int cableCount)
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

        var device = new VacDevice(deviceInfoSet, new SP_DEVINFO_DATA { cbSize = (uint)sizeof(SP_DEVINFO_DATA) });
        try
        {
            if (!SetupDiCreateDeviceInfo(deviceInfoSet, new string(className), ref classGuid, null, 0, DICD_GENERATE_ID, ref device._deviceInfoData))
            {
                throw LastError(nameof(SetupDiCreateDeviceInfo));
            }

            // REG_MULTI_SZ
            var hardwareIds = Encoding.Unicode.GetBytes(HardwareId + "\0\0");
            fixed (byte* buffer = hardwareIds)
            {
                if (!SetupDiSetDeviceRegistryProperty(deviceInfoSet, ref device._deviceInfoData, SPDRP_HARDWAREID, buffer, (uint)hardwareIds.Length))
                {
                    throw LastError(nameof(SetupDiSetDeviceRegistryProperty));
                }
            }

            if (!SetupDiCallClassInstaller(DIF_REGISTERDEVICE, deviceInfoSet, ref device._deviceInfoData))
            {
                throw LastError("SetupDiCallClassInstaller(DIF_REGISTERDEVICE)");
            }

            // before the driver starts; the INF keeps an existing value
            var key = SetupDiCreateDevRegKey(deviceInfoSet, ref device._deviceInfoData, DICS_FLAG_GLOBAL, 0, DIREG_DEV, 0, null);
            if (key == InvalidHandle)
            {
                throw LastError(nameof(SetupDiCreateDevRegKey));
            }

            WriteCableCount(key, cableCount);
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
    public static VacDevice? Find()
    {
        VacDevice? found = null;
        ForEach(device =>
        {
            if (found == null || (!found.IsPresent && device.IsPresent))
            {
                found = device;
            }
        }, out var deviceInfoSet);

        if (found == null)
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
            return null;
        }

        return new VacDevice(deviceInfoSet, found._deviceInfoData);
    }

    /// <summary>
    /// Removes every devnode of the device, including leftovers. Returns whether a reboot is needed to finish.
    /// </summary>
    public static bool RemoveAll()
    {
        var needReboot = false;
        ForEach(device =>
        {
            if (!DiUninstallDevice(0, device._deviceInfoSet, ref device._deviceInfoData, 0, out var reboot))
            {
                throw LastError(nameof(DiUninstallDevice));
            }

            needReboot |= reboot;
        }, out var deviceInfoSet);

        SetupDiDestroyDeviceInfoList(deviceInfoSet);
        return needReboot;
    }

    public void Dispose()
    {
        SetupDiDestroyDeviceInfoList(_deviceInfoSet);
    }

    public int GetCableCount()
    {
        var key = SetupDiOpenDevRegKey(_deviceInfoSet, ref _deviceInfoData, DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_READ);
        if (key == InvalidHandle)
        {
            return 1;
        }

        using var handle = new SafeRegistryHandle(key, true);
        using var registryKey = RegistryKey.FromHandle(handle);
        return registryKey.GetValue(CableCountValue) is int count ? Math.Clamp(count, 1, MaxCableCount) : 1;
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

    /// <summary>
    /// Restarts the device so the driver reads its settings again. Returns whether a reboot is needed instead, e.g. while an
    /// application keeps a cable open.
    /// </summary>
    public bool Restart()
    {
        var parameters = new SP_PROPCHANGE_PARAMS
        {
            ClassInstallHeader = new SP_CLASSINSTALL_HEADER { cbSize = (uint)sizeof(SP_CLASSINSTALL_HEADER), InstallFunction = DIF_PROPERTYCHANGE },
            StateChange = DICS_PROPCHANGE,
            Scope = DICS_FLAG_GLOBAL,
        };

        if (!SetupDiSetClassInstallParams(_deviceInfoSet, ref _deviceInfoData, ref parameters, (uint)sizeof(SP_PROPCHANGE_PARAMS)))
        {
            throw LastError(nameof(SetupDiSetClassInstallParams));
        }

        if (!SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, _deviceInfoSet, ref _deviceInfoData))
        {
            throw LastError("SetupDiCallClassInstaller(DIF_PROPERTYCHANGE)");
        }

        var installParams = new SP_DEVINSTALL_PARAMS { cbSize = (uint)sizeof(SP_DEVINSTALL_PARAMS) };
        return SetupDiGetDeviceInstallParams(_deviceInfoSet, ref _deviceInfoData, ref installParams)
            && (installParams.Flags & (DI_NEEDREBOOT | DI_NEEDRESTART)) != 0;
    }

    public void SetCableCount(int count)
    {
        var key = SetupDiOpenDevRegKey(_deviceInfoSet, ref _deviceInfoData, DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_SET_VALUE);
        if (key == InvalidHandle)
        {
            throw LastError(nameof(SetupDiOpenDevRegKey));
        }

        WriteCableCount(key, count);
    }

    private static void ForEach(Action<VacDevice> action, out nint deviceInfoSet)
    {
        deviceInfoSet = SetupDiGetClassDevs(0, "ROOT", 0, DIGCF_ALLCLASSES);
        if (deviceInfoSet == InvalidHandle)
        {
            throw LastError(nameof(SetupDiGetClassDevs));
        }

        var data = new SP_DEVINFO_DATA { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };
        for (uint i = 0; SetupDiEnumDeviceInfo(deviceInfoSet, i, ref data); i++)
        {
            var device = new VacDevice(deviceInfoSet, data);
            if (device.HasHardwareId())
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

    private static void WriteCableCount(nint key, int count)
    {
        using var handle = new SafeRegistryHandle(key, true);
        using var registryKey = RegistryKey.FromHandle(handle);
        registryKey.SetValue(CableCountValue, Math.Clamp(count, 1, MaxCableCount), RegistryValueKind.DWord);
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
            if (!SetupDiGetDeviceProperty(_deviceInfoSet, ref _deviceInfoData, propertyKey, out _, pointer, size, out _, 0))
            {
                return null;
            }
        }

        return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }

    private bool HasHardwareId()
    {
        SetupDiGetDeviceRegistryProperty(_deviceInfoSet, ref _deviceInfoData, SPDRP_HARDWAREID, out _, null, 0, out var size);
        if (size == 0)
        {
            return false;
        }

        var buffer = new byte[size];
        fixed (byte* pointer = buffer)
        {
            if (!SetupDiGetDeviceRegistryProperty(_deviceInfoSet, ref _deviceInfoData, SPDRP_HARDWAREID, out _, pointer, size, out _))
            {
                return false;
            }
        }

        return Encoding.Unicode.GetString(buffer).Split('\0').Contains(HardwareId, StringComparer.OrdinalIgnoreCase);
    }
}
