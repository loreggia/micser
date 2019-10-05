using Micser.Common;
using Micser.Common.Audio;
using Micser.Common.Settings;
using Micser.Engine.Api;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Micser.Engine.Test.Api
{
    public class EngineProcessorTest
    {
        [Fact]
        public async Task GetStatus()
        {
            var audioEngine = new Mock<IAudioEngine>();
            var settingsService = new Mock<ISettingsService>();

            audioEngine.SetupGet(x => x.IsRunning).Returns(true);

            var processor = new EngineProcessor(audioEngine.Object, settingsService.Object);

            var result = await processor.ProcessAsync("getstatus", null).ConfigureAwait(false);

            audioEngine.VerifyGet(e => e.IsRunning, Times.Once);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RestartEngine()
        {
            var audioEngine = new Mock<IAudioEngine>();
            var settingsService = new Mock<ISettingsService>();

            audioEngine.Setup(e => e.Start()).Verifiable();
            audioEngine.Setup(e => e.Stop()).Verifiable();

            var processor = new EngineProcessor(audioEngine.Object, settingsService.Object);

            var result = await processor.ProcessAsync("restart", null).ConfigureAwait(false);

            audioEngine.Verify(e => e.Stop(), Times.Once);
            audioEngine.Verify(e => e.Start(), Times.Once);
            settingsService.Verify(e => e.SetSettingAsync(It.Is<string>(s => s == Globals.SettingKeys.IsEngineRunning), It.Is<bool>(x => x)));

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task StartEngine()
        {
            var audioEngine = new Mock<IAudioEngine>();
            var settingsService = new Mock<ISettingsService>();

            audioEngine.Setup(e => e.Start()).Verifiable();
            settingsService.Setup(s => s.SetSettingAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);

            var processor = new EngineProcessor(audioEngine.Object, settingsService.Object);

            var result = await processor.ProcessAsync("start", null).ConfigureAwait(false);

            audioEngine.Verify(e => e.Start(), Times.Once);
            settingsService.Verify(e => e.SetSettingAsync(It.Is<string>(s => s == Globals.SettingKeys.IsEngineRunning), It.Is<bool>(x => x)));

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task StopEngine()
        {
            var audioEngine = new Mock<IAudioEngine>();
            audioEngine.Setup(e => e.Start()).Verifiable();

            var settingsService = new Mock<ISettingsService>();

            var processor = new EngineProcessor(audioEngine.Object, settingsService.Object);

            var result = await processor.ProcessAsync("stop", null).ConfigureAwait(false);

            audioEngine.Verify(e => e.Stop(), Times.Once);
            settingsService.Verify(e => e.SetSettingAsync(It.Is<string>(s => s == Globals.SettingKeys.IsEngineRunning), It.Is<bool>(x => x == false)));

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }
    }
}