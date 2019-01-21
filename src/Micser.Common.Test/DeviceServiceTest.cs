using System;
using System.Linq;
using Micser.Common.Devices;
using Xunit;

namespace Micser.Infrastructure.Test
{
    public class DeviceServiceTest
    {
        [Fact]
        //[Category("Sound")]
        public void GetInputDevicesTest()
        {
            var service = new DeviceService();
            var devices = service.GetDevices(DeviceType.Input);
            var devicesArray = devices.ToArray();

            Assert.True(devicesArray.Length > 0);
            foreach (var deviceDescription in devicesArray)
            {
                Console.WriteLine($"{deviceDescription.Name}: {deviceDescription.Id}");
            }
        }

        [Fact]
        //[TestCategory("Sound")]
        public void GetOutputDevicesTest()
        {
            var service = new DeviceService();
            var devices = service.GetDevices(DeviceType.Output);
            var devicesArray = devices.ToArray();

            Assert.True(devicesArray.Length > 0);
            foreach (var deviceDescription in devicesArray)
            {
                Console.WriteLine($"{deviceDescription.Name}: {deviceDescription.Id}");
            }
        }
    }
}