using CSCore.CoreAudioAPI;
using CSCore.Win32;
using Micser.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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

            RenameDevices(devicesR);
            RenameDevices(devicesC);
        }

        private void RenameDevices(List<MMDevice> devices)
        {
            var rxTopologyName = new Regex(@"\\wave(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            devices.Sort((d1, d2) =>
            {
                // HACK - this property is not documented my microsoft
                var topologyInfo1 = d1.PropertyStore.GetValue(DriverGlobals.PropertyKeys.TopologyInfo).GetValue()?.ToString();
                var topologyInfo2 = d2.PropertyStore.GetValue(DriverGlobals.PropertyKeys.TopologyInfo).GetValue()?.ToString();

                if (!string.IsNullOrEmpty(topologyInfo1) &&
                    !string.IsNullOrEmpty(topologyInfo2))
                {
                    var match1 = rxTopologyName.Match(topologyInfo1);
                    var match2 = rxTopologyName.Match(topologyInfo2);

                    if (match1.Success && match2.Success)
                    {
                        var value1 = match1.Groups.Count > 1 ? match1.Groups[1].Value : match1.Value;
                        var value2 = match2.Groups.Count > 1 ? match2.Groups[1].Value : match2.Value;

                        return string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase);
                    }
                }

                return 0;
            });

            for (var i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                var deviceName = $"{Globals.DeviceInterfaceName} {i + 1}";
                var pName = Marshal.StringToHGlobalUni(deviceName);
                var propertyStore = device.OpenPropertyStore(StorageAccess.ReadWrite);
                propertyStore.SetValue(
                    DriverGlobals.PropertyKeys.DeviceDescription,
                    new PropertyVariant { DataType = VarEnum.VT_LPWSTR, PointerValue = pName });
                propertyStore.Commit();
            }
        }
    }
}