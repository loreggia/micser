using Micser.Common.Modules;
using Micser.Common.Widgets;
using Newtonsoft.Json;
using System;
using System.Windows;
using Xunit;

namespace Micser.Common.Test
{
    public class SerializationTest
    {
        [Fact]
        public void Serialize_Deserialize_ModuleDescription()
        {
            var description = new ModuleDescription
            {
                Id = Guid.NewGuid(),
                ModuleState = new ModuleState(),
                ModuleType = "TestType1",
                WidgetState = new WidgetState
                {
                    Position = new Point(1, 2),
                    Size = new Size(3, 4)
                },
                WidgetType = "TestType2"
            };
            description.ModuleState.Data["Test1"] = 1;
            description.WidgetState.Data["Test2"] = 2;

            var serialized = JsonConvert.SerializeObject(description, Formatting.Indented);

            var result = JsonConvert.DeserializeObject<ModuleDescription>(serialized);

            Assert.NotNull(result);
            Assert.NotNull(result.ModuleState);
            Assert.NotNull(result.WidgetState);

            Assert.Equal(description.Id, result.Id);
            Assert.Equal("TestType1", result.ModuleType);
            Assert.Equal("TestType2", result.WidgetType);

            Assert.Equal(1, result.WidgetState.Position.X);
            Assert.Equal(2, result.WidgetState.Position.Y);
            Assert.Equal(3, result.WidgetState.Size.Width);
            Assert.Equal(4, result.WidgetState.Size.Height);

            Assert.Equal(1L, result.ModuleState.Data["Test1"]);
            Assert.Equal(2L, result.WidgetState.Data["Test2"]);
        }
    }
}