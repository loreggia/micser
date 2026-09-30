using CSCore.CoreAudioAPI;
using CSCore.Win32;
using Micser.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<bool> RenameDevices(int expectedCount)
        {
            const int numRetries = 10;
            var retries = 0;

            do
            {
                var renderDevices = GetDevices(DataFlow.Render);
                var captureDevices = GetDevices(DataFlow.Capture);

                if (renderDevices.Count == expectedCount && captureDevices.Count == expectedCount)
                {
                    try
                    {
                        RenameDevices(renderDevices);
                        RenameDevices(captureDevices);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        return false;
                    }
                }

                Logger.Info($"Expected number of devices: {expectedCount}, current number of devices: {renderDevices.Count} (render), {captureDevices.Count} (capture). Retrying ({retries + 1}/{numRetries})...");
                await Task.Delay(2000);

                retries++;
            } while (retries < numRetries);

            return false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _deviceEnumerator?.Dispose();
            }
        }

        private List<MMDevice> GetDevices(DataFlow dataFlow)
        {
            var devices = _deviceEnumerator.EnumAudioEndpoints(dataFlow, DeviceState.Active | DeviceState.Disabled);

            var result = new List<MMDevice>();

            foreach (var device in devices)
            {
                var deviceNameProperty = device.PropertyStore.GetValue(Globals.PropertyKeys.DeviceName);
                var deviceName = deviceNameProperty.GetValue()?.ToString();

                if (deviceName == Globals.DeviceInterfaceName)
                {
                    result.Add(device);
                }
            }

            return result;
        }

        private void RenameDevices(List<MMDevice> devices)
        {
            var rxTopologyName = new Regex(@"\\wave(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            devices.Sort((d1, d2) =>
            {
                // HACK - this property is not documented my microsoft
                var topologyInfo1 = d1.PropertyStore.GetValue(Globals.PropertyKeys.TopologyInfo).GetValue()?.ToString();
                var topologyInfo2 = d2.PropertyStore.GetValue(Globals.PropertyKeys.TopologyInfo).GetValue()?.ToString();

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
                    Globals.PropertyKeys.DeviceDescription,
                    new PropertyVariant { DataType = VarEnum.VT_LPWSTR, PointerValue = pName });
                propertyStore.Commit();
            }
        }
    }
}