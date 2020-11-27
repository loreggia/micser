using System;
using System.Collections.Generic;
using System.IO;
using Micser.Common.Settings;
using Micser.TestCommon;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Micser.App.Infrastructure.Test.Settings
{
    public class SettingsSerializerTest : IDisposable
    {
        private readonly TestFileManager _testFileManager;
        private readonly ITestOutputHelper _testOutputHelper;

        public SettingsSerializerTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _testFileManager = new TestFileManager(testOutputHelper);
        }

        public void Dispose()
        {
            _testFileManager.DeleteFiles();
        }

        [Fact]
        public void LoadFromFile()
        {
            var fileName = _testFileManager.GetFileName();
            const string json = "{\"DecimalKey\":1.23,\"IntKey\":1,\"ObjectKey\":{\"Property\":\"Value\"},\"StringKey\":\"Value\"}";
            File.WriteAllText(fileName, json);

            IDictionary<string, object> settings = new Dictionary<string, object>();
            var settingsServiceMock = new Mock<ISettingsService>();
            settingsServiceMock
                .Setup(s => s.SetSettingAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, object>((key, value) => settings.Add(key, value));

            var importer = new SettingsSerializer(new TestLogger<SettingsSerializer>(_testOutputHelper), settingsServiceMock.Object);
            var result = importer.Import(fileName);

            Assert.True(result);
            Assert.Contains("DecimalKey", settings);
            Assert.Contains("IntKey", settings);
            Assert.Contains("ObjectKey", settings);
            Assert.Contains("StringKey", settings);
            Assert.Equal(1.23d, settings["DecimalKey"]);
            Assert.Equal(1L, settings["IntKey"]);
            Assert.IsType<JObject>(settings["ObjectKey"]);
            Assert.Equal("Value", settings["StringKey"]);
        }

        [Fact]
        public void WriteToFile()
        {
            var fileName = _testFileManager.GetFileName();

            var settingsServiceMock = new Mock<ISettingsService>();
            settingsServiceMock.Setup(s => s.GetSettings()).Returns(new Dictionary<string, object>
            {
                {"DecimalKey", 1.23d},
                {"IntKey", 1},
                {"ObjectKey", new {Property = "Value"}},
                {"StringKey", "Value"}
            });

            var exporter = new SettingsSerializer(new TestLogger<SettingsSerializer>(_testOutputHelper), settingsServiceMock.Object);

            var result = exporter.Export(fileName);

            Assert.True(result);
            Assert.True(File.Exists(fileName));

            var fileContent = File.ReadAllText(fileName);
            Assert.Equal("{\"DecimalKey\":1.23,\"IntKey\":1,\"ObjectKey\":{\"Property\":\"Value\"},\"StringKey\":\"Value\"}", fileContent);
        }
    }
}