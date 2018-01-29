using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.Main.Audio;
using Micser.Main.Services;

namespace Micser.Main.Test.Services
{
    [TestClass]
    public class DeviceServiceTest
    {
        [TestMethod]
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