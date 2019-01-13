using CSCore.CoreAudioAPI;
using System.Collections.Generic;

namespace Micser.Common.Devices
{
    public class DeviceService
    {
        public IEnumerable<DeviceDescription> GetDevices(DeviceType type)
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            foreach (var audioEndPoint in deviceEnumerator.EnumAudioEndpoints(type == DeviceType.Input ? DataFlow.Capture : DataFlow.Render, DeviceState.Active | DeviceState.UnPlugged))
            {
                yield return new DeviceDescription
                {
                    Id = audioEndPoint.DeviceID,
                    Name = audioEndPoint.FriendlyName,
                    //IconPath = audioEndPoint.IconPath,
                    IsActive = audioEndPoint.DeviceState == DeviceState.Active,
                    Type = type
                };
            }
        }
    }
}