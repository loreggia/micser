using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.Main.Audio;

namespace Micser.Main.Test.Audio
{
    [TestClass]
    public class DeviceInputTest
    {
        [TestMethod]
        public void SetDeviceDescriptionStartsRecording()
        {
            var deviceInput = new DeviceInput();

            var deviceDescription = new DeviceDescription
            {
                // Microphone (Sound Blaster Z) on Nemesis
                Id = "{0.0.1.00000000}.{21f57bfb-8de1-44a6-be5c-c744fb839641}"
            };

            deviceInput.DeviceDescription = deviceDescription;

            var testOutput = new TestChainLink();
            testOutput.Input = deviceInput;

            for (int i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);
                if (testOutput.HasInput)
                {
                    break;
                }
            }

            //todo create meaningful test after change to output driven
            //Assert.IsTrue(testOutput.HasInput);

            deviceInput.Dispose();
        }

        private class TestChainLink : AudioChainLink
        {
            public bool HasInput { get; set; }
        }
    }
}