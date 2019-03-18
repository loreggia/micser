using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using Micser.Engine.Infrastructure.Audio;
using System;
using System.Collections.Concurrent;
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

        public interface IAudioModule
        {
            long Id { get; }

            void AddOutput(IAudioModule module);

            void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count);
        }

        [Fact]
        public async Task Test()
        {
            var inputModule = new DeviceInputModule(1);
            var inputModule2 = new DeviceInputModule(2);
            var outputModule = new DeviceOutputModule(3);

            inputModule.AddOutput(outputModule);
            inputModule2.AddOutput(outputModule);

            await Task.Delay(20000);
        }

        public abstract class AudioModule : IAudioModule, IDisposable
        {
            private readonly IList<IAudioModule> _outputs;

            protected AudioModule(long id)
            {
                Id = id;
                _outputs = new List<IAudioModule>(2);
            }

            ~AudioModule()
            {
                Dispose(false);
            }

            public long Id { get; }

            public virtual void AddOutput(IAudioModule module)
            {
                if (_outputs.Contains(module))
                {
                    return;
                }

                _outputs.Add(module);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public virtual void RemoveOutput(IAudioModule module)
            {
                if (_outputs.Contains(module))
                {
                    _outputs.Remove(module);
                }
            }

            public virtual void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count)
            {
                foreach (var module in _outputs)
                {
                    module.Write(source, waveFormat, buffer, offset, count);
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

        public class DeviceInputModule : AudioModule
        {
            private WasapiCapture _capture;

            public DeviceInputModule(long id)
                : base(id)
            {
                string deviceId;

                using (var deviceEnumerator = new MMDeviceEnumerator())
                {
                    var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                    deviceId = device.DeviceID;
                }

                SetDevice(deviceId);
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

            private void SetDevice(string deviceId)
            {
                if (string.IsNullOrEmpty(deviceId) || _capture != null && _capture.Device.DeviceID != deviceId)
                {
                    _capture.Stop();
                    _capture.Dispose();
                    _capture = null;
                }

                using (var deviceEnumerator = new MMDeviceEnumerator())
                {
                    var device = deviceEnumerator.GetDevice(deviceId);

                    if (device == null)
                    {
                        return;
                    }

                    _capture = new WasapiCapture(true, AudioClientShareMode.Shared) { Device = device };

                    _capture.Initialize();
                    _capture.DataAvailable += (s, e) =>
                    {
                        Write(this, e.Format, e.Data, e.Offset, e.ByteCount);
                    };
                    _capture.Start();
                }
            }
        }

        public class DeviceOutputModule : AudioModule
        {
            private readonly IDictionary<long, WriteableBufferingSource> _inputBuffers;
            private readonly IDictionary<long, ISampleSource> _inputSources;
            private int _channelCount;
            private WasapiOut _output;
            private MixerSampleSource _outputBuffer;

            public DeviceOutputModule(long id)
                : base(id)
            {
                _inputBuffers = new ConcurrentDictionary<long, WriteableBufferingSource>();
                _inputSources = new ConcurrentDictionary<long, ISampleSource>();

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

            public override void AddOutput(IAudioModule module)
            {
                throw new InvalidOperationException();
            }

            public override void RemoveOutput(IAudioModule module)
            {
            }

            public override void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count)
            {
                if (!_inputBuffers.ContainsKey(source.Id) || !waveFormat.Equals(_inputBuffers[source.Id].WaveFormat))
                {
                    SetInputBuffer(source.Id, waveFormat);
                }

                _inputBuffers[source.Id].Write(buffer, offset, count);
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

            private void SetDevice(string deviceId)
            {
                if (string.IsNullOrEmpty(deviceId) || _output != null && _output.Device.DeviceID != deviceId)
                {
                    _output.Stop();
                    _output.Dispose();
                    _output = null;
                }

                using (var deviceEnumerator = new MMDeviceEnumerator())
                {
                    var device = deviceEnumerator.GetDevice(deviceId);

                    if (device == null)
                    {
                        return;
                    }

                    _output = new WasapiOut(true, AudioClientShareMode.Shared, 1) { Device = device };
                }
            }

            private void SetInputBuffer(long id, WaveFormat format)
            {
                if (_inputBuffers.ContainsKey(id))
                {
                    _outputBuffer.RemoveSource(_inputSources[id]);
                }

                _inputBuffers[id] = new WriteableBufferingSource(format);

                if (_outputBuffer == null || format.Channels > _channelCount)
                {
                    SetOutputBuffer(format);
                }

                _inputSources[id] = _inputBuffers[id].ToSampleSource();
                _outputBuffer.AddSource(_inputSources[id]);
            }

            private void SetOutputBuffer(WaveFormat format)
            {
                var restart = _outputBuffer != null;

                _outputBuffer = new MixerSampleSource(format.Channels, format.SampleRate) { DivideResult = false, FillWithZeros = true };

                foreach (var sampleSource in _inputSources.Values)
                {
                    _outputBuffer.AddSource(sampleSource);
                }

                if (restart)
                {
                    void OnStopped(object sender, PlaybackStoppedEventArgs e)
                    {
                        _output.Initialize(_outputBuffer.ToWaveSource());
                        _output.Stopped -= OnStopped;
                        _output.Play();
                    }

                    _output.Stopped += OnStopped;
                    _output.Stop();
                }
                else
                {
                    _output.Initialize(_outputBuffer.ToWaveSource());
                    _output.Play();
                }

                _channelCount = format.Channels;
            }
        }
    }
}