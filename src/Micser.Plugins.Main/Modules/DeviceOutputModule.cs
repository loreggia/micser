using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CSCore.Streams;
using Micser.Engine.Infrastructure.Audio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Micser.Plugins.Main.Modules
{
    public class DeviceOutputModule : DeviceModule
    {
        private readonly IDictionary<long, WriteableBufferingSource> _inputBuffers;
        private readonly IDictionary<long, ISampleSource> _inputSources;
        private int _channelCount;
        private int _latency;

        private WasapiOut _output;

        private MixerSampleSource _outputBuffer;

        public DeviceOutputModule(long id)
            : base(id)
        {
            _inputBuffers = new ConcurrentDictionary<long, WriteableBufferingSource>();
            _inputSources = new ConcurrentDictionary<long, ISampleSource>();

            Latency = 1;
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
                _outputBuffer?.Dispose();
                _inputBuffers.Clear();
                _inputSources.Clear();
            }

            base.Dispose(disposing);
        }

        protected override void DisposeDevice()
        {
            if (_output != null)
            {
                _output.Stop();
                _output.Dispose();
                _output = null;
            }

            base.DisposeDevice();
        }

        protected override void OnInitializeDevice()
        {
            _output = new WasapiOut(true, AudioClientShareMode.Shared, Latency) { Device = Device };

            if (_outputBuffer != null)
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

            _inputBuffers[id] = new WriteableBufferingSource(format);

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
                    if (Device.DeviceState == DeviceState.Active)
                    {
                        _output.Initialize(_outputBuffer.ToWaveSource());
                        _output.Stopped -= OnStopped;
                        _output.Play();
                    }
                }

                _output.Stopped += OnStopped;
                _output.Stop();
            }
            else if (_output != null && Device.DeviceState == DeviceState.Active)
            {
                _output.Initialize(_outputBuffer.ToWaveSource());
                _output.Play();
            }

            _channelCount = format.Channels;
        }
    }
}