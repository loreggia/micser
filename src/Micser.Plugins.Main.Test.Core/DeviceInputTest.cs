﻿using CSCore;
using CSCore.CoreAudioAPI;
using Micser.Common.Api;
using Micser.Common.Devices;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Services;
using Micser.Plugins.Main.Modules;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Micser.Plugins.Main.Test
{
    public class DeviceInputTest
    {
        [Fact]
        [Trait("Category", "Sound")]
        public async Task TestActualInput()
        {
            var apiEndPointMock = new Mock<IApiEndPoint>();
            apiEndPointMock.Setup(e => e.SendMessageAsync(It.IsAny<JsonRequest>())).ReturnsAsync(new JsonResponse(true));
            var moduleServiceMock = new Mock<IModuleService>();
            moduleServiceMock.Setup(e => e.GetById(It.IsAny<long>())).Returns<long>(id => new ModuleDto { Id = id, State = new ModuleState() });

            var module = new DeviceInputModule(apiEndPointMock.Object, moduleServiceMock.Object);

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
            {
                _writeCallback = writeCallback;
            }

            public override void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count)
            {
                _writeCallback(source, waveFormat, buffer, offset, count);
            }
        }
    }
}