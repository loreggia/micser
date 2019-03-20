using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CSCore.Streams;
using Micser.Common.Devices;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Audio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Plugins.Main.Modules
{
    public class DeviceOutputModule : AudioModule
    {
        private const string DeviceIdKey = "DeviceId";
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly IDictionary<long, WriteableBufferingSource> _inputBuffers;
        private readonly IDictionary<long, ISampleSource> _inputSources;
        private int _channelCount;
        private DeviceDescription _deviceDescription;
        private int _latency;

        private WasapiOut _output;

        private MixerSampleSource _outputBuffer;

        public DeviceOutputModule(long id)
            : base(id)
        {
            _inputBuffers = new ConcurrentDictionary<long, WriteableBufferingSource>();
            _inputSources = new ConcurrentDictionary<long, ISampleSource>();

            _deviceEnumerator = new MMDeviceEnumerator();
            _deviceEnumerator.DeviceStateChanged += OnDeviceStateChanged;

            Latency = 5;
        }

        /// <summary>
        ///     Gets or sets the output device description. The <see cref="DeviceDescription.Id" />-Property is used to select the device.
        /// </summary>
        public DeviceDescription DeviceDescription
        {
            get => _deviceDescription;
            set
            {
                var oldId = _deviceDescription?.Id;
                _deviceDescription = value;

                if (oldId != _deviceDescription?.Id)
                {
                    InitializeDevice();
                }
            }
        }

        /// <summary>
        ///     Gets or sets the output latency in milliseconds.
        /// </summary>
        public int Latency
        {
            get => _latency;
            set
            {
                if (_latency != value)
                {
                    _latency = value;
                    InitializeDevice();
                }
            }
        }

        public override void AddOutput(IAudioModule module)
        {
            throw new InvalidOperationException();
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            var deviceId = state?.Data.GetObject<string>(DeviceIdKey);
            if (deviceId != null)
            {
                var deviceService = new DeviceService();
                DeviceDescription = deviceService.GetDevices(DeviceType.Output).FirstOrDefault(d => d.Id == deviceId);
            }
        }

        public override void RemoveOutput(IAudioModule module)
        {
            throw new InvalidOperationException();
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
                _deviceEnumerator.Dispose();
                _output?.Dispose();
                _outputBuffer?.Dispose();
                _inputBuffers.Clear();
                _inputSources.Clear();
            }

            base.Dispose(disposing);
        }

        protected virtual void OnDeviceStateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            if (_output != null &&
                _outputBuffer != null &&
                _output.Device.DeviceID == e.DeviceId &&
                e.DeviceState == DeviceState.Active)
            {
                _output.Initialize(_outputBuffer.ToWaveSource());
                _output.Play();
            }
        }

        private void InitializeDevice()
        {
            if (string.IsNullOrEmpty(DeviceDescription?.Id) || _output != null && _output.Device.DeviceID != DeviceDescription.Id)
            {
                _output?.Stop();
                _output?.Dispose();
                _output = null;
            }

            if (DeviceDescription == null)
            {
                return;
            }

            var device = _deviceEnumerator.GetDevice(DeviceDescription.Id);

            if (device == null)
            {
                return;
            }

            _output = new WasapiOut(true, AudioClientShareMode.Shared, Latency) { Device = device };

            if (_outputBuffer != null && device.DeviceState == DeviceState.Active)
            {
                _output.Initialize(_outputBuffer.ToWaveSource());
                _output.Play();
            }
        }

        private void SetInputBuffer(long id, WaveFormat format)
        {
            if (_inputBuffers.ContainsKey(id))
            {
                _outputBuffer.RemoveSource(_inputSources[id]);
            }

            _inputBuffers[id] = new MyWriteableBufferingSource(format);

            if (_outputBuffer == null || format.Channels > _channelCount)
            {
                SetOutputBuffer(format);
            }

            if (_inputSources.ContainsKey(id))
            {
                _outputBuffer.RemoveSource(_inputSources[id]);
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
                    if (_output.Device.DeviceState == DeviceState.Active)
                    {
                        _output.Initialize(_outputBuffer.ToWaveSource());
                        _output.Stopped -= OnStopped;
                        _output.Play();
                    }
                }

                _output.Stopped += OnStopped;
                _output.Stop();
            }
            else if (_output != null && _output.Device.DeviceState == DeviceState.Active)
            {
                _output.Initialize(_outputBuffer.ToWaveSource());
                _output.Play();
            }

            _channelCount = format.Channels;
        }
    }

    internal class MyWriteableBufferingSource : WriteableBufferingSource
    {
        public MyWriteableBufferingSource(WaveFormat waveFormat) : base(waveFormat)
        {
        }

        public MyWriteableBufferingSource(WaveFormat waveFormat, int bufferSize) : base(waveFormat, bufferSize)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}