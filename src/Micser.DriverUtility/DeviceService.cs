using CSCore.CoreAudioAPI;
using CSCore.Win32;
using Micser.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Micser.DriverUtility
{
    public class DeviceService : IDisposable
    {
        private static readonly ILogger Logger;
        private readonly MMDeviceEnumerator _deviceEnumerator;

        static DeviceService()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public DeviceService()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
        }

        public void Dispose()
        {
            _deviceEnumerator.Dispose();
        }

        public void RenameDevices()
        {
            var devices = _deviceEnumerator.EnumAudioEndpoints(DataFlow.All, DeviceState.Active | DeviceState.Disabled);

            var interfaceNamePropertyKey = new PropertyKey(new Guid("b3f8fa53-0004-438e-9003-51a46e139bfc"), 6);

            var devicesR = new List<MMDevice>();
            var devicesC = new List<MMDevice>();

            foreach (var device in devices)
            {
                var interfaceNameProperty = device.PropertyStore.GetValue(interfaceNamePropertyKey);
                var interfaceName = interfaceNameProperty.GetValue()?.ToString();

                if (interfaceName == Globals.DeviceInterfaceName)
                {
                    if (device.DataFlow.HasFlag(DataFlow.Capture))
                    {
                        devicesC.Add(device);
                    }
                    else if (device.DataFlow.HasFlag(DataFlow.Render))
                    {
                        devicesR.Add(device);
                    }
                }
            }

            var defMmDeviceR = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var defComDeviceR = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications);
            var defConDeviceR = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            RenameDevices(devicesR);
        }

        private void RenameDevices(
            IList<MMDevice> devices
        //,MMDevice defaultMultimediaDevice,
        //MMDevice defaultCommunicationDevice,
        //MMDevice defaultConsoleDevice
        )
        {
            for (var i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                var deviceName = Globals.DeviceInterfaceName + (i + 1);
                var pName = Marshal.StringToHGlobalUni(deviceName);
                // todo access denied??
                device.PropertyStore.SetValue(PropertyStore.FriendlyName, new PropertyVariant { DataType = VarEnum.VT_LPWSTR, PointerValue = pName });
            }
        }
    }
}