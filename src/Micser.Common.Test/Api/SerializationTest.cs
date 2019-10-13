using ProtoBuf;
using System.IO;
using Xunit;

namespace Micser.Common.Test.Api
{
    public class SerializationTest
    {
        [Fact]
        public void SerializeDynamic_String_IsSuccess()
        {
            var thing = new Thing
            {
                Key = "Key",
                Value = "StringValue"
            };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, thing);

            ms.Position = 0;
            var result = Serializer.Deserialize<Thing>(ms);

            Assert.NotNull(result);
            Assert.Equal("Key", result.Key);
            Assert.Equal("StringValue", result.Value);
        }

        [ProtoContract]
        public class Thing
        {
            [ProtoMember(1)]
            public string Key { get; set; }

            [ProtoMember(2, DynamicType = true)]
            public object Value { get; set; }
        }
    }
}