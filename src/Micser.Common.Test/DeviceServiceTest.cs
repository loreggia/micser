using Micser.Common.Devices;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test
{
    public class DeviceServiceTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public DeviceServiceTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        [Trait("Category", "Sound")]
        public void GetInputDevicesTest()
        {
            var service = new DeviceService();
            var devices = service.GetDevices(DeviceType.Input);
            var devicesArray = devices.ToArray();

            Assert.True(devicesArray.Length > 0);
            foreach (var deviceDescription in devicesArray)
            {
                _testOutputHelper.WriteLine($"{deviceDescription.FriendlyName}: {deviceDescription.Id}");
            }
        }

        [Fact]
        [Trait("Category", "Sound")]
        public void GetOutputDevicesTest()
        {
            var service = new DeviceService();
            var devices = service.GetDevices(DeviceType.Output);
            var devicesArray = devices.ToArray();

            Assert.True(devicesArray.Length > 0);
            foreach (var deviceDescription in devicesArray)
            {
                _testOutputHelper.WriteLine($"{deviceDescription.FriendlyName}: {deviceDescription.Id}");
            }
        }
    }
}