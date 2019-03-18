using CSCore.CoreAudioAPI;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Common.Devices
{
    public class DeviceService
    {
        public DeviceDescription GetDescription(MMDevice device)
        {
            if (device == null)
            {
                return null;
            }

            return new DeviceDescription
            {
                Id = device.DeviceID,
                Name = device.FriendlyName,
                //IconPath = audioEndPoint.IconPath,
                IsActive = device.DeviceState == DeviceState.Active,
                Type = device.DataFlow == DataFlow.Capture ? DeviceType.Input : DeviceType.Output
            };
        }

        public DeviceDescription GetDescription(string id)
        {
            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                var device = deviceEnumerator.GetDevice(id);
                return GetDescription(device);
            }
        }

        public IEnumerable<DeviceDescription> GetDevices(DeviceType type)
        {
            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                return deviceEnumerator.EnumAudioEndpoints(
                    type == DeviceType.Input
                        ? DataFlow.Capture
                        : DataFlow.Render,
                    DeviceState.Active | DeviceState.UnPlugged)
                    .Select(GetDescription)
                    .ToArray();
            }
        }
    }
}