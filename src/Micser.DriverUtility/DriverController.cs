using Microsoft.Win32;
using Micser.Common;
using NLog;
using System;

namespace Micser.DriverUtility
{
    public class DriverController
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public int SetDeviceSettingsAndReload(uint deviceCount)
        {
            if (deviceCount > DriverGlobals.MaxDeviceCount)
            {
                deviceCount = DriverGlobals.MaxDeviceCount;
            }

            try
            {
                var registryKey = Registry.CurrentUser.CreateSubKey(Globals.UserRegistryRoot, true);
                registryKey.SetValue(DriverGlobals.RegistryValues.DeviceCount, deviceCount, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not save the settings to the registry.");
                return Globals.DriverUtility.ReturnCodes.RegistryAccessFailed;
            }

            try
            {
                var result = Globals.DriverUtility.ReturnCodes.Success;

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while sending the reload control signal to the driver.");
                return Globals.DriverUtility.ReturnCodes.SendControlSignalFailed;
            }
        }
    }
}