using CSCore.CoreAudioAPI;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Common.Devices
{
    /// <summary>
    /// Provides helper methods for enumerating or getting hardware audio device information.
    /// </summary>
    public class DeviceService
    {
        /// <summary>
        /// Gets an internal <see cref="DeviceDescription"/> from a WASAPI <see cref="MMDevice"/> instance.
        /// </summary>
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
                //IconPath = device.IconPath,
                IsActive = device.DeviceState == DeviceState.Active,
                Type = device.DataFlow == DataFlow.Capture ? DeviceType.Input : DeviceType.Output
            };
        }

        /// <summary>
        /// Gets a <see cref="DeviceDescription"/> for the specified hardware device ID.
        /// </summary>
        public DeviceDescription GetDescription(string id)
        {
            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                var device = deviceEnumerator.GetDevice(id);
                return GetDescription(device);
            }
        }

        /// <summary>
        /// Enumerates active or unplugged devices of the specified type.
        /// </summary>
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