using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.Shared.Models;
using Newtonsoft.Json;

namespace Micser.Shared.Test
{
    [TestClass]
    public class SerializationTest
    {
        [TestMethod]
        public void SerializeAudioModuleDescription()
        {
            var description = new AudioModuleDescription
            {
                Id = 1,
                Type = typeof(Module)
            };
            description.SetState(new ModuleState { Name = "test" });

            var json = JsonConvert.SerializeObject(description);

            var deserialized = JsonConvert.DeserializeObject<AudioModuleDescription>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(description.Id, deserialized.Id);
            Assert.AreEqual(description.Type, deserialized.Type);

            var state = deserialized.GetState<ModuleState>();
            Assert.IsNotNull(state);
            Assert.AreEqual("test", state.Name);
        }

        private class Module
        {
            public string Name { get; set; }
        }

        private class ModuleState
        {
            public string Name { get; set; }
        }
    }
}