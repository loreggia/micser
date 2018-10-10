using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.Infrastructure.Models;
using Micser.Infrastructure.Modules;
using System.Threading;

namespace Micser.Engine.Test.Audio
{
    [TestClass]
    public class DeviceOutputTest
    {
        [TestMethod]
        [TestCategory("Sound")]
        [TestCategory("SkipWhenLiveUnitTesting")]
        public void PlaySineWave()
        {
            var deviceOutput = new DeviceOutputModule();

            var deviceDescription = new DeviceDescription
            {
                // Speakers (Sound Blaster Z) on Nemesis
                Id = "{0.0.0.00000000}.{c8f64c30-5862-45d5-8ef0-8f592e950eb9}"
            };

            deviceOutput.DeviceDescription = deviceDescription;

            //var sineGenerator = new WaveGenerator();
            //deviceOutput.Input = sineGenerator;

            Thread.Sleep(1000);

            deviceOutput.Dispose();
        }

        [TestMethod]
        [TestCategory("Sound")]
        [TestCategory("SkipWhenLiveUnitTesting")]
        public void PlaySquareWave()
        {
            var deviceOutput = new DeviceOutputModule();

            var deviceDescription = new DeviceDescription
            {
                // Speakers (Sound Blaster Z) on Nemesis
                Id = "{0.0.0.00000000}.{c8f64c30-5862-45d5-8ef0-8f592e950eb9}"
            };

            deviceOutput.DeviceDescription = deviceDescription;

            //var waveGenerator = new WaveGenerator
            //{
            //    Type = WaveType.Square,
            //    Frequency = 220
            //};
            //deviceOutput.Input = waveGenerator;

            Thread.Sleep(1000);

            deviceOutput.Dispose();
        }

        /// <summary>
        /// This should not create a sound!
        /// </summary>
        [TestMethod]
        [TestCategory("Sound")]
        public void SetDescriptionToNullResetsOutput()
        {
            var deviceOutput = new DeviceOutputModule();

            var deviceDescription = new DeviceDescription
            {
                // Speakers (Sound Blaster Z) on Nemesis
                Id = "{0.0.0.00000000}.{c8f64c30-5862-45d5-8ef0-8f592e950eb9}"
            };

            deviceOutput.DeviceDescription = deviceDescription;

            deviceOutput.DeviceDescription = null;

            //var sineGenerator = new WaveGenerator();
            //deviceOutput.Input = sineGenerator;

            Thread.Sleep(1000);

            deviceOutput.Dispose();
        }

        [TestMethod]
        [TestCategory("Sound")]
        [ExpectedException(typeof(CoreAudioAPIException))]
        public void SetInvalidDescriptionInvalidIdThrowsException()
        {
            var deviceOutput = new DeviceOutputModule
            {
                Input = new TestInput()
            };

            var deviceDescription = new DeviceDescription
            {
                Id = "asdflkjasldkfjlaskf"
            };

            deviceOutput.DeviceDescription = deviceDescription;
        }

        [TestMethod]
        [TestCategory("Sound")]
        public void SetInvalidDescriptionNullIdDoesNothing()
        {
            var deviceOutput = new DeviceOutputModule();

            var deviceDescription = new DeviceDescription();

            deviceOutput.DeviceDescription = deviceDescription;
        }

        private class TestInput : AudioModule
        {
            public TestInput()
            {
                Output = new WriteableBufferingSource(new WaveFormat()) { FillWithZeros = true };
            }

            public override string GetState()
            {
                return null;
            }
        }
    }
}