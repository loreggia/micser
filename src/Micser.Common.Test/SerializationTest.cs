using Micser.Common.Extensions;
using Micser.Common.Modules;
using Newtonsoft.Json;
using Xunit;

namespace Micser.Common.Test
{
    public class SerializationTest
    {
        [Fact]
        public void Serialize_Deserialize_ModuleDescription()
        {
            var description = new ModuleDto
            {
                Id = 123,
                ModuleType = "TestType1",
                State = new ModuleState
                {
                    Data =
                    {
                        {"Position", "1,2"},
                        {"Size", "3,4"}
                    }
                },
                WidgetType = "TestType2"
            };

            var serialized = JsonConvert.SerializeObject(description, Formatting.Indented);

            var result = JsonConvert.DeserializeObject<ModuleDto>(serialized);

            Assert.NotNull(result);
            Assert.NotNull(result.State);

            Assert.Equal(123, result.Id);
            Assert.Equal("TestType1", result.ModuleType);
            Assert.Equal("TestType2", result.WidgetType);

            Assert.Equal("1,2", result.State.Data.GetObject<string>("Position"));
            Assert.Equal("3,4", result.State.Data.GetObject<string>("Size"));
        }
    }
}