using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Micser.Main.Test.Audio
{
    [TestClass]
    public class LowLevelTest
    {
        [TestMethod]
        public void EventDriven()
        {
            var capture = new WasapiCapture(true, AudioClientShareMode.Shared);
            capture.Initialize();

            var inSource = new SoundInSource(capture) { FillWithZeros = true };

            var output = new WasapiOut(true, AudioClientShareMode.Shared, 20);
            output.Initialize(inSource);

            capture.Start();
            output.Play();
        }
    }
}