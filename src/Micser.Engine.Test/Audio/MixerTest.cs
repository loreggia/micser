using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using Micser.Infrastructure.Common.Models;

namespace Micser.Engine.Test.Audio
{
    [TestClass]
    public class MixerTest
    {
        [TestMethod]
        [TestCategory("Sound")]
        [TestCategory("SkipWhenLiveUnitTesting")]
        public void MixSineWavesToDeviceOutput()
        {
            var deviceOutput = new DeviceOutputModule();

            var deviceDescription = new DeviceDescription
            {
                // Speakers (Sound Blaster Z) on Nemesis
                Id = "{0.0.0.00000000}.{c8f64c30-5862-45d5-8ef0-8f592e950eb9}"
            };

            deviceOutput.DeviceDescription = deviceDescription;

            // A
            //var sineGenerator1 = new WaveGenerator { Frequency = 440 };
            //// C
            //var sineGenerator2 = new WaveGenerator { Frequency = 523.251 };
            //// E
            //var sineGenerator3 = new WaveGenerator { Frequency = 659.255 };

            //var mixer1 = new Mixer
            //{
            //    Input = sineGenerator1,
            //    Input2 = sineGenerator2
            //};

            //var mixer2 = new Mixer
            //{
            //    Input = mixer1,
            //    Input2 = sineGenerator3
            //};

            //deviceOutput.Volume = 0.1f;
            //deviceOutput.Input = mixer2;

            Thread.Sleep(1000);

            deviceOutput.Dispose();
        }
    }
}