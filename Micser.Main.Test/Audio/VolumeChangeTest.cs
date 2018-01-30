using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.Main.Audio;

namespace Micser.Main.Test.Audio
{
    [TestClass]
    public class VolumeChangeTest
    {
        [TestMethod]
        [TestCategory("SkipWhenLiveUnitTesting")]
        public void ChangeVolumeOfSineGeneratorToDeviceOutput()
        {
            var deviceOutput = new DeviceOutput();

            var deviceDescription = new DeviceDescription
            {
                // Speakers (Sound Blaster Z) on Nemesis
                Id = "{0.0.0.00000000}.{c8f64c30-5862-45d5-8ef0-8f592e950eb9}"
            };

            deviceOutput.DeviceDescription = deviceDescription;

            var sineGenerator = new WaveGenerator
            {
                Frequency = 220
            };

            deviceOutput.Input = sineGenerator;

            for (int i = 0; i < 500; i++)
            {
                sineGenerator.Volume = i * 0.001f;
                Thread.Sleep(10);
            }

            deviceOutput.Dispose();
        }
    }
}