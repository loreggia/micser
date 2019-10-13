using Micser.Common.Settings;
using Micser.Engine.Api;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Micser.Engine.Test.Api
{
    public class SettingsProcessorTest
    {
        [Fact]
        public async Task GetSetting()
        {
            var settingsServiceMock = new Mock<ISettingsService>();

            settingsServiceMock.Setup(x => x.GetSetting(It.IsAny<string>())).Returns("value").Verifiable();

            var processor = new SettingsProcessor(settingsServiceMock.Object);

            var result = await processor.ProcessAsync("getsetting", "key");

            settingsServiceMock.Verify(x => x.GetSetting(It.Is<string>(key => key == "key")));

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);

            var dto = result.Content as SettingValueDto;
            Assert.NotNull(dto);
            Assert.Equal("key", dto.Key);
            Assert.Equal("value", dto.Value);
        }

        [Fact]
        public async Task SetSetting()
        {
            var settingsServiceMock = new Mock<ISettingsService>();

            settingsServiceMock.Setup(x => x.GetSetting(It.IsAny<string>())).Returns("value");
            settingsServiceMock.Setup(x => x.SetSettingAsync(It.IsAny<string>(), It.IsAny<object>())).Verifiable();

            var processor = new SettingsProcessor(settingsServiceMock.Object);

            var dto = new SettingValueDto { Key = "key", Value = "value" };
            var result = await processor.ProcessAsync("setsetting", dto);

            settingsServiceMock.Verify(x => x.SetSettingAsync(It.Is<string>(key => key == "key"), It.Is<string>(val => val == "value")));

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            var resultDto = result.Content as SettingValueDto;
            Assert.NotNull(resultDto);
            Assert.Equal("value", resultDto.Value);
        }
    }
}