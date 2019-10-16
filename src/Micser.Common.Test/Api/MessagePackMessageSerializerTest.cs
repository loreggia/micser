using Micser.Common.Api;
using Micser.TestCommon;
using NLog;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.Api
{
    public class MessagePackMessageSerializerTest
    {
        public MessagePackMessageSerializerTest(ITestOutputHelper testOutputHelper)
        {
            TestOutputLogger.Configure(testOutputHelper);
        }

        [Fact]
        public async Task SerializeDynamicObject_IsSuccess()
        {
            var thing = new Thing
            {
                Key = "Key",
                Value = new Thing { Key = "Nested", Value = true }
            };
            using var ms = new MemoryStream();

            var serializer = new MessagePackMessageSerializer();
            await serializer.SerializeAsync(ms, thing);

            LogManager.GetCurrentClassLogger().Info($"Data size: {ms.Position}");

            ms.Position = 0;
            var result = await serializer.DeserializeAsync<Thing>(ms);

            Assert.NotNull(result);
            Assert.Equal("Key", result.Key);
            var nested = result.Value as Thing;
            Assert.NotNull(nested);
            Assert.Equal("Nested", nested.Key);
            Assert.Equal(true, nested.Value);
        }

        [Theory]
        [ClassData(typeof(SerializerTestData))]
        public async Task SerializeDynamicPrimitive_IsSuccess(object value)
        {
            var thing = new Thing
            {
                Key = "Key",
                Value = value
            };
            using var ms = new MemoryStream();

            var serializer = new MessagePackMessageSerializer();
            await serializer.SerializeAsync(ms, thing);

            LogManager.GetCurrentClassLogger().Info($"Data size: {ms.Position}");

            ms.Position = 0;
            var result = await serializer.DeserializeAsync<Thing>(ms);

            Assert.NotNull(result);
            Assert.Equal("Key", result.Key);
            Assert.Equal(value, result.Value);
        }

        public class SerializerTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return ToArray("String");
                yield return ToArray(42);
                yield return ToArray(69d);
                yield return ToArray(true);
                yield return ToArray((object)null);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private static object[] ToArray(params object[] values)
            {
                return values;
            }
        }

        public class Thing
        {
            public string Key { get; set; }

            public object Value { get; set; }
        }
    }
}