using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Engine.Test.Audio
{
    public class GeneralAudioTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public GeneralAudioTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public interface IModule
        {
            void AddInput(IModule module);

            void AddOutput(IModule module);

            void RemoveInput(IModule module);

            void RemoveOutput(IModule module);

            void Write(WaveFormat waveFormat, byte[] buffer, int offset, int count);
        }

        [Fact]
        public async Task Test()
        {
            var inputModule = new DeviceInputModule();
            var inputModule2 = new DeviceInputModule();
            var outputModule = new DeviceOutputModule();

            inputModule.AddOutput(outputModule);
            inputModule2.AddOutput(outputModule);

            await Task.Delay(20000);
        }

        public class DeviceInputModule : Module
        {
            private readonly WasapiCapture _capture;

            public DeviceInputModule()
            {
                using (var deviceEnumerator = new MMDeviceEnumerator())
                {
                    var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);

                    _capture = new WasapiCapture(true, AudioClientShareMode.Shared) { Device = device };

                    _capture.Initialize();
                    _capture.DataAvailable += (s, e) =>
                    {
                        Write(e.Format, e.Data, e.Offset, e.ByteCount);
                    };
                    _capture.Start();
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _capture.Stop();
                    _capture.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        public class DeviceOutputModule : Module
        {
            private readonly WasapiOut _output;

            private WriteableBufferingSource _outputBuffer;

            public DeviceOutputModule()
            {
                using (var deviceEnumerator = new MMDeviceEnumerator())
                {
                    var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

                    _output = new WasapiOut(true, AudioClientShareMode.Shared, 1) { Device = device };
                    _output.Stopped += (s, e) =>
                    {
                        Console.WriteLine("Output stopped");
                    };
                }
            }

            public override void AddOutput(IModule module)
            {
                throw new InvalidOperationException();
            }

            public override void RemoveOutput(IModule module)
            {
                throw new InvalidOperationException();
            }

            public override void Write(WaveFormat waveFormat, byte[] buffer, int offset, int count)
            {
                if (_outputBuffer == null || !waveFormat.Equals(_outputBuffer.WaveFormat))
                {
                    SetBuffer(waveFormat);
                }

                _outputBuffer.Write(buffer, offset, count);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _output.Dispose();
                    _outputBuffer.Dispose();
                }

                base.Dispose(disposing);
            }

            private void SetBuffer(WaveFormat format)
            {
                var restart = _outputBuffer != null;

                _outputBuffer = new WriteableBufferingSource(format);

                if (restart)
                {
                    void OnStopped(object sender, PlaybackStoppedEventArgs e)
                    {
                        _output.Initialize(_outputBuffer);
                        _output.Stopped -= OnStopped;
                        _output.Play();
                    }

                    _output.Stopped += OnStopped;
                    _output.Stop();
                }
                else
                {
                    _output.Initialize(_outputBuffer);
                    _output.Play();
                }
            }
        }

        public abstract class Module : IModule, IDisposable
        {
            private readonly IList<IModule> _inputs;
            private readonly IList<IModule> _outputs;

            protected Module()
            {
                _inputs = new List<IModule>(2);
                _outputs = new List<IModule>(2);
            }

            ~Module()
            {
                Dispose(false);
            }

            public virtual void AddInput(IModule module)
            {
                if (_inputs.Contains(module))
                {
                    return;
                }

                _inputs.Add(module);
                module.AddOutput(this);
            }

            public virtual void AddOutput(IModule module)
            {
                if (_outputs.Contains(module))
                {
                    return;
                }

                _outputs.Add(module);
                module.AddInput(this);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public virtual void RemoveInput(IModule module)
            {
                _inputs.Remove(module);
                module.RemoveOutput(this);
            }

            public virtual void RemoveOutput(IModule module)
            {
                _outputs.Remove(module);
                module.RemoveInput(this);
            }

            public virtual void Write(WaveFormat waveFormat, byte[] buffer, int offset, int count)
            {
                foreach (var module in _outputs)
                {
                    module.Write(waveFormat, buffer, offset, count);
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _outputs.Clear();
                }
            }
        }
    }
}