using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Micser.Infrastructure.Models;

namespace Micser.Infrastructure.Test
{
    [TestClass]
    public class DeviceServiceTest
    {
        [TestMethod]
        [TestCategory("Sound")]
        public void GetInputDevicesTest()
        {
            var service = new DeviceService();
            var devices = service.GetDevices(DeviceType.Input);
            var devicesArray = devices.ToArray();

            Assert.IsTrue(devicesArray.Length > 0);
            foreach (var deviceDescription in devicesArray)
            {
                Console.WriteLine($"{deviceDescription.Name}: {deviceDescription.Id}");
            }
        }

        [TestMethod]
        [TestCategory("Sound")]
        public void GetOutputDevicesTest()
        {
            var service = new DeviceService();
            var devices = service.GetDevices(DeviceType.Output);
            var devicesArray = devices.ToArray();

            Assert.IsTrue(devicesArray.Length > 0);
            foreach (var deviceDescription in devicesArray)
            {
                Console.WriteLine($"{deviceDescription.Name}: {deviceDescription.Id}");
            }
        }
    }
}