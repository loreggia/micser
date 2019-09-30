using Xunit;

namespace Micser.Common.Test
{
    public class ArgumentDictionaryTest
    {
        [Fact]
        public void CreateWithDefaultNameCharsAndArray()
        {
            var args = new[] { "--flag", "-name1", "value1", "-name2", "/value2" };
            var dict = new ArgumentDictionary(args);
            Assert.True(dict.HasFlag("flag"));
        }

        [Fact]
        public void CreateWithDefaultNameCharsAndParams()
        {
            var dict = new ArgumentDictionary("--flag", "-name1", "value1", "-name2", "/value2");
            Assert.True(dict.HasFlag("flag"));
        }

        [Fact]
        public void CreateWithMultipleNameChars()
        {
            var dict = new ArgumentDictionary(new[] { "-", "/", "--" }, "--flag", "-name1", "value1", "-name2", "/value2");
            Assert.True(dict.HasFlag("flag"));
            Assert.Equal("value1", dict["name1"]);
            Assert.Null(dict["name2"]);
            Assert.True(dict.HasFlag("name2"));
            Assert.True(dict.HasFlag("value2"));
        }

        [Fact]
        public void CreateWithSingleNameChar()
        {
            var dict = new ArgumentDictionary(new[] { "-" }, "-flag", "-name1", "value1", "-name2", "/value2");
            Assert.True(dict.HasFlag("flag"));
            Assert.False(dict.HasFlag("name2"));
            Assert.False(dict.HasFlag("value2"));
            Assert.Equal("value1", dict["name1"]);
            Assert.Equal("/value2", dict["name2"]);
        }

        [Fact]
        public void GetString()
        {
            var dict = new ArgumentDictionary("--flag1", "/Flag2", "-name1", "value1", "-name2", "value2");
            Assert.Equal("Flags: flag1, flag2 | Parameters: [name1=value1], [name2=value2]", dict.ToString());
        }

        [Fact]
        public void NameIsCaseInsensitive()
        {
            var dict = new ArgumentDictionary(new[] { "-" }, "-flag", "-name1", "value1");
            Assert.True(dict.HasFlag("Flag"));
            Assert.Equal("value1", dict["Name1"]);
        }

        [Fact]
        public void ParametersAreNotFlags()
        {
            var dict = new ArgumentDictionary("-flag", "-key", "value", "-key2", "value2");
            Assert.True(dict.HasFlag("flag"));
            Assert.Null(dict["flag"]);
            Assert.False(dict.HasFlag("key"));
            Assert.False(dict.HasFlag("value"));
            Assert.False(dict.HasFlag("key2"));
            Assert.False(dict.HasFlag("value2"));
        }
    }
}