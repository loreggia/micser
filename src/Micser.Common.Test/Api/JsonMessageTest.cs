using Micser.Common.Api;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.Api
{
    public class JsonMessageTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public JsonMessageTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void DeserializeMessage()
        {
            var json = "{\"Content\":{\"IntProperty\":42,\"StringProperty\":\"Test\"},\"ContentType\":\"Micser.Common.Test.Api.JsonMessageTest+TestClass, Micser.Common.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"IsSuccess\":true}";

            var message = JsonConvert.DeserializeObject<JsonResponse>(json);

            Assert.NotNull(message);
            Assert.NotNull(message.Content);
            Assert.True(message.IsSuccess);
            Assert.IsType<TestClass>(message.Content);
            var content = (TestClass)message.Content;
            Assert.Equal("Test", content.StringProperty);
            Assert.Equal(42, content.IntProperty);
        }

        [Fact]
        public void SerializeMessage()
        {
            var message = new JsonResponse
            {
                IsSuccess = true,
                Content = new TestClass
                {
                    StringProperty = "Test",
                    IntProperty = 42
                }
            };

            var messageJson = JsonConvert.SerializeObject(message);
            _testOutputHelper.WriteLine(messageJson);

            Assert.Contains("\"Content\":{", messageJson);
            Assert.Contains("\"IntProperty\":42", messageJson);
            Assert.Contains("\"StringProperty\":\"Test\"", messageJson);
            Assert.Contains("\"ContentType\":\"Micser.Common.Test.Api.JsonMessageTest+TestClass, Micser.Common.Test, Version=", messageJson);
            Assert.Contains("\"IsSuccess\":true", messageJson);
        }

        private class TestClass
        {
            public int IntProperty { get; set; }
            public string StringProperty { get; set; }
        }
    }
}