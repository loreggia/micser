using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.Main.Audio;

namespace Micser.Main.Test.Audio
{
    [TestClass]
    public class DeviceOutputTest
    {
        [TestMethod]
        public void PlaySineWave()
        {
            var deviceOutput = new DeviceOutput();

            var deviceDescription = new DeviceDescription
            {
                // Speakers (Sound Blaster Z) on Nemesis
                Id = "{0.0.0.00000000}.{c8f64c30-5862-45d5-8ef0-8f592e950eb9}"
            };

            deviceOutput.DeviceDescription = deviceDescription;

            var sineGenerator = new SineGenerator();
            deviceOutput.Input = sineGenerator;

            sineGenerator.Start();

            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(10);
            }

            sineGenerator.Stop();
            deviceOutput.Dispose();
        }
    }
}