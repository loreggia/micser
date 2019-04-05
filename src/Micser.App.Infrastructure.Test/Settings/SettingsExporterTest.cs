using Micser.App.Infrastructure.Settings;
using Moq;
using Newtonsoft.Json.Linq;
using NLog;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Micser.App.Infrastructure.Test.Settings
{
    public class SettingsExporterTest
    {
        [Fact]
        public void LoadFromFile()
        {
            const string fileName = "loadTest.json";
            const string json = "{\"DecimalKey\":1.23,\"IntKey\":1,\"ObjectKey\":{\"Property\":\"Value\"},\"StringKey\":\"Value\"}";
            File.WriteAllText(fileName, json);

            IDictionary<string, object> settings = new Dictionary<string, object>();
            var settingsServiceMock = new Mock<ISettingsService>();
            settingsServiceMock
                .Setup(s => s.SetSetting(It.IsAny<string>(), It.IsAny<object>()))
                .Callback<string, object>((key, value) => settings.Add(key, value));

            var importer = new SettingsExporter(new NullLogger(new LogFactory()), settingsServiceMock.Object);
            var result = importer.Load(fileName);

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
            const string fileName = "saveTest.json";

            var settingsServiceMock = new Mock<ISettingsService>();
            settingsServiceMock.Setup(s => s.GetSettings()).Returns(new Dictionary<string, object>
            {
                {"DecimalKey", 1.23d},
                {"IntKey", 1},
                {"ObjectKey", new {Property = "Value"}},
                {"StringKey", "Value"}
            });

            var exporter = new SettingsExporter(new NullLogger(new LogFactory()), settingsServiceMock.Object);

            var result = exporter.Save(fileName);

            Assert.True(result);
            Assert.True(File.Exists(fileName));

            var fileContent = File.ReadAllText(fileName);
            Assert.Equal("{\"DecimalKey\":1.23,\"IntKey\":1,\"ObjectKey\":{\"Property\":\"Value\"},\"StringKey\":\"Value\"}", fileContent);
        }
    }
}