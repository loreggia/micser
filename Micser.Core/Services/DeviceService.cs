using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace Micser.Core.Services
{
    public class DeviceService
    {
        public IEnumerable<DeviceDescription> GetInputDevices()
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            foreach (var audioEndPoint in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                yield return new DeviceDescription
                {
                    Id = audioEndPoint.ID,
                    Name = audioEndPoint.FriendlyName,
                    IconPath = audioEndPoint.IconPath,
                    IsActive = audioEndPoint.State == DeviceState.Active,
                    Type = DeviceType.Input
                };
            }
        }
    }
}