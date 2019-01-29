using Micser.Common.Modules;
using Micser.Common.Widgets;
using Newtonsoft.Json;
using System;
using System.Windows;
using Xunit;

namespace Micser.Infrastructure.Test
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
                    Size = new Size()
                },
                WidgetType = "TestType2"
            };
            description.ModuleState.Add("Test1", 1);
            description.WidgetState.Add("Test2", 2);

            var serialized = JsonConvert.SerializeObject(description);

            var result = JsonConvert.DeserializeObject<ModuleDescription>(serialized);

            Assert.NotNull(result);
        }
    }
}