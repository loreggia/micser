using CSCore.CoreAudioAPI;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Engine.Test.Audio
{
    public class VolumeChangeTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public VolumeChangeTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task DeviceEnumeratorPropertyChange()
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            var volume = AudioEndpointVolume.FromDevice(device);
            var callback = new AudioEndpointVolumeCallback();
            callback.NotifyRecived += (s, e) =>
            {
                _testOutputHelper.WriteLine($"{e.IsMuted} - {e.MasterVolume}");
            };
            volume.RegisterControlChangeNotify(callback);

            await Task.Delay(5000);

            enumerator.Dispose();
        }
    }
}