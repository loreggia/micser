using CSCore;
using CSCore.CoreAudioAPI;
using Micser.Common.Devices;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Modules;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Micser.Plugins.Main.Test
{
    public class DeviceInputTest
    {
        [Fact]
        public async Task TestActualInput()
        {
            var module = new DeviceInputModule(1);

            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                var defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);

                module.DeviceDescription = new DeviceService().GetDescription(defaultDevice);
            }

            IAudioModule resultSource = null;
            WaveFormat resultWaveFormat = null;
            byte[] resultBuffer = null;
            var resultOffset = 0;
            var resultCount = 0;

            TestOutputModule testOutput = null;
            testOutput = new TestOutputModule((source, waveFormat, buffer, offset, count) =>
            {
                module.RemoveOutput(testOutput);
                resultSource = source;
                resultWaveFormat = waveFormat;
                resultBuffer = buffer;
                resultOffset = offset;
                resultCount = count;
            });

            module.AddOutput(testOutput);

            await Task.Delay(1000);

            Assert.Same(module, resultSource);
            Assert.NotNull(resultWaveFormat);
            Assert.NotNull(resultBuffer);
            Assert.True(resultCount > 0);
        }

        private class TestOutputModule : AudioModule
        {
            private readonly Action<IAudioModule, WaveFormat, byte[], int, int> _writeCallback;

            public TestOutputModule(Action<IAudioModule, WaveFormat, byte[], int, int> writeCallback)
                : base(-1)
            {
                _writeCallback = writeCallback;
            }

            public override ModuleState GetState()
            {
                return null;
            }

            public override void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count)
            {
                _writeCallback(source, waveFormat, buffer, offset, count);
            }
        }
    }
}