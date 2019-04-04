using Micser.App.Infrastructure.Settings;
using Moq;
using NLog;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Micser.App.Infrastructure.Test.Settings
{
    public class SettingsExporterTest
    {
        [Fact]
        public void WriteToFile()
        {
            var settingsServiceMock = new Mock<ISettingsService>();
            settingsServiceMock.Setup(s => s.GetSettings()).Returns(new Dictionary<string, object>
            {
                {"DecimalKey", 1.23d},
                {"IntKey", 1},
                {"ObjectKey", new {Property = "Value"}},
                {"StringKey", "Value"}
            });

            var exporter = new SettingsExporter(new NullLogger(new LogFactory()), settingsServiceMock.Object);

            var result = exporter.Save("test.json");

            Assert.True(result);
            Assert.True(File.Exists("test.json"));

            var fileContent = File.ReadAllText("test.json");
            Assert.Equal("{\"DecimalKey\":1.23,\"IntKey\":1,\"ObjectKey\":{\"Property\":\"Value\"},\"StringKey\":\"Value\"}", fileContent);
        }
    }
}