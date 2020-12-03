using System.Linq;
using Micser.Common.Audio;
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

            Assert.NotNull(devicesArray);
            foreach (var deviceDescription in devicesArray)
            {
                Assert.NotNull(deviceDescription.Id);
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

            Assert.NotNull(devicesArray);
            foreach (var deviceDescription in devicesArray)
            {
                Assert.NotNull(deviceDescription.Id);
                _testOutputHelper.WriteLine($"{deviceDescription.FriendlyName}: {deviceDescription.Id}");
            }
        }
    }
}