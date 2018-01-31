﻿using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.Main.Audio;

namespace Micser.Main.Test.Audio
{
    [TestClass]
    public class DeviceOutputTest
    {
        [TestMethod]
        [TestCategory("SkipWhenLiveUnitTesting")]
        public void PlaySineWave()
        {
            var deviceOutput = new DeviceOutput();

            var deviceDescription = new DeviceDescription
            {
                // Speakers (Sound Blaster Z) on Nemesis
                Id = "{0.0.0.00000000}.{c8f64c30-5862-45d5-8ef0-8f592e950eb9}"
            };

            deviceOutput.DeviceDescription = deviceDescription;

            var sineGenerator = new WaveGenerator();
            deviceOutput.Input = sineGenerator;

            Thread.Sleep(1000);

            deviceOutput.Dispose();
        }

        [TestMethod]
        [TestCategory("SkipWhenLiveUnitTesting")]
        public void PlaySquareWave()
        {
            var deviceOutput = new DeviceOutput();

            var deviceDescription = new DeviceDescription
            {
                // Speakers (Sound Blaster Z) on Nemesis
                Id = "{0.0.0.00000000}.{c8f64c30-5862-45d5-8ef0-8f592e950eb9}"
            };

            deviceOutput.DeviceDescription = deviceDescription;

            var waveGenerator = new WaveGenerator
            {
                Type = WaveType.Square,
                Frequency = 220
            };
            deviceOutput.Input = waveGenerator;

            Thread.Sleep(1000);

            deviceOutput.Dispose();
        }

        /// <summary>
        /// This should not create a sound!
        /// </summary>
        [TestMethod]
        public void SetDescriptionToNullResetsOutput()
        {
            var deviceOutput = new DeviceOutput();

            var deviceDescription = new DeviceDescription
            {
                // Speakers (Sound Blaster Z) on Nemesis
                Id = "{0.0.0.00000000}.{c8f64c30-5862-45d5-8ef0-8f592e950eb9}"
            };

            deviceOutput.DeviceDescription = deviceDescription;

            deviceOutput.DeviceDescription = null;

            var sineGenerator = new WaveGenerator();
            deviceOutput.Input = sineGenerator;

            Thread.Sleep(1000);

            deviceOutput.Dispose();
        }

        /// <summary>
        /// TODO specific exception?
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SetInvalidDescriptionInvalidIdThrowsException()
        {
            var deviceOutput = new DeviceOutput();

            var deviceDescription = new DeviceDescription
            {
                Id = "asdflkjasldkfjlaskf"
            };

            deviceOutput.DeviceDescription = deviceDescription;
        }

        [TestMethod]
        public void SetInvalidDescriptionNullIdDoesNothing()
        {
            var deviceOutput = new DeviceOutput();

            var deviceDescription = new DeviceDescription();

            deviceOutput.DeviceDescription = deviceDescription;
        }
    }
}