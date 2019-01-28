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
                State = new ModuleState
                {
                    Data = { { "Test1", 1 } },
                },
                Type = "TestType",
                ViewState = new WidgetState
                {
                    Data = { { "Test2", 2 } },
                    Position = new Point(1, 2),
                    Size = new Size()
                }
            };

            var serialized = JsonConvert.SerializeObject(description);

            var result = JsonConvert.DeserializeObject<ModuleDescription>(serialized);

            Assert.NotNull(result);
        }
    }
}