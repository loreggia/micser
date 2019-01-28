using Micser.Common;
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
                State = new StateDictionary
                {
                    { "Test1", 1 }
                },
                Type = "TestType",
                ViewState = new WidgetState
                {
                    Position = new Point(1, 2),
                    Size = new Size()
                }
            };
            description.ViewState.Add("Test2", 2);

            var serialized = JsonConvert.SerializeObject(description);

            var result = JsonConvert.DeserializeObject<ModuleDescription>(serialized);

            Assert.NotNull(result);
        }
    }
}