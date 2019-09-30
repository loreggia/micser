using System.IO;
using Xunit;

namespace Micser.App.Infrastructure.Test
{
    public class IniFileTest
    {
        [Fact]
        public void LoadInvalidFileThrowsException()
        {
            Assert.Throws<FileNotFoundException>(() => new IniFile("asdf.ini"));
        }

        [Fact]
        public void LoadValidFile()
        {
            var iniFile = new IniFile("Test.ini");

            Assert.Equal("1", iniFile.GetValue("section1", "key1"));
            Assert.Equal("Value2", iniFile.GetValue("Section1", "Key2"));
            Assert.Equal("Text value", iniFile.GetValue("Section1", "Key3"));

            Assert.Equal("11", iniFile.GetValue("Section2", "Key1"));
            Assert.Null(iniFile.GetValue("Section2", "Key2"));
            Assert.Equal("Null", iniFile.GetValue("Section2", "Key3"));
        }
    }
}