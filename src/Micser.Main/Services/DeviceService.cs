using System.Collections.Generic;
using Micser.Main.Audio;
using NAudio.CoreAudioApi;

namespace Micser.Main.Services
{
    public class DeviceService
    {
        public IEnumerable<DeviceDescription> GetDevices(DeviceType type)
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            foreach (var audioEndPoint in deviceEnumerator.EnumerateAudioEndPoints(type == DeviceType.Input ? DataFlow.Capture : DataFlow.Render, DeviceState.Active))
            {
                yield return new DeviceDescription
                {
                    Id = audioEndPoint.ID,
                    Name = audioEndPoint.FriendlyName,
                    IconPath = audioEndPoint.IconPath,
                    IsActive = audioEndPoint.State == DeviceState.Active,
                    Type = type
                };
            }
        }
    }
}