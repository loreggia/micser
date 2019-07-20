using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.DSP;
using CSCore.SoundOut;
using CSCore.Streams;
using Micser.Common.Api;
using Micser.Common.Devices;
using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using WriteableBufferingSource = Micser.Plugins.Main.Audio.WriteableBufferingSource;

namespace Micser.Plugins.Main.Modules
{
    public class DeviceOutputModule : DeviceModule
    {
        private readonly IDictionary<long, WriteableBufferingSource> _inputBuffers;
        private readonly IDictionary<long, ISampleSource> _inputSources;
        private int _channelCount;
        private bool _isMuted;
        private int _latency;
        private WasapiOut _output;
        private MixerSampleSource _outputMixer;
        private float _volume;
        private VolumeSource _volumeSource;

        public DeviceOutputModule(IApiEndPoint apiEndPoint, IModuleService moduleService)
            : base(apiEndPoint, moduleService)
        {
            _inputBuffers = new ConcurrentDictionary<long, WriteableBufferingSource>();
            _inputSources = new ConcurrentDictionary<long, ISampleSource>();

            Latency = 1;
        }

        public override bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;

                if (_volumeSource != null)
                {
                    _volumeSource.Volume = value ? 0f : _volume;
                }
            }
        }

        /// <summary>
        /// Gets or sets the output latency in milliseconds.
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

        public override float Volume
        {
            get => _volume;
            set
            {
                _volume = value;

                if (_volumeSource != null && !IsMuted)
                {
                    _volumeSource.Volume = value;
                }
            }
        }

        protected override DeviceType DeviceType => DeviceType.Output;

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
            if (_volumeSource != null)
            {
                _volumeSource.Volume = IsMuted ? 0f : _volume;
            }

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
                _volumeSource?.Dispose();
                _outputMixer?.Dispose();
                _inputBuffers?.Clear();
                _inputSources?.Clear();
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
            var resetBuffer = _output == null;
            _output = new WasapiOut(true, AudioClientShareMode.Shared, Latency) { Device = Device };

            if (_outputMixer != null)
            {
                if (resetBuffer)
                {
                    foreach (var buffer in _inputBuffers.Values)
                    {
                        buffer.Clear();
                    }
                }

                _output.Initialize(_volumeSource.ToWaveSource());
                _output.Play();
            }
        }

        private void SetInputBuffer(long id, WaveFormat format)
        {
            if (_inputBuffers.ContainsKey(id))
            {
                _outputMixer.RemoveSource(_inputSources[id]);
            }

            _inputBuffers[id] = new WriteableBufferingSource(format);

            if (_outputMixer == null || format.Channels > _channelCount)
            {
                SetOutputBuffer(format);
            }

            if (_inputSources.ContainsKey(id))
            {
                _outputMixer.RemoveSource(_inputSources[id]);
            }

            IWaveSource waveSource = _inputBuffers[id];

            if (waveSource.WaveFormat.SampleRate != _outputMixer.WaveFormat.SampleRate)
            {
                waveSource = waveSource.ChangeSampleRate(_outputMixer.WaveFormat.SampleRate);
            }

            if (waveSource.WaveFormat.Channels != _outputMixer.WaveFormat.Channels)
            {
                waveSource = new DmoChannelResampler(waveSource, ChannelMatrix.GetMatrix(waveSource.WaveFormat, _outputMixer.WaveFormat));
            }

            _inputSources[id] = waveSource.ToSampleSource();

            _outputMixer.AddSource(_inputSources[id]);
        }

        private void SetOutputBuffer(WaveFormat format)
        {
            var restart = _outputMixer != null;

            _outputMixer = new MixerSampleSource(format.Channels, format.SampleRate) { DivideResult = false, FillWithZeros = true };
            _volumeSource = new VolumeSource(_outputMixer) { DisposeBaseSource = false };

            foreach (var sampleSource in _inputSources.Values)
            {
                _outputMixer.AddSource(sampleSource);
            }

            if (restart)
            {
                void OnStopped(object sender, PlaybackStoppedEventArgs e)
                {
                    _output.Stopped -= OnStopped;

                    if (Device.DeviceState == DeviceState.Active)
                    {
                        _output.Initialize(_volumeSource.ToWaveSource());
                        _output.Play();
                    }
                }

                _output.Stopped += OnStopped;
                _output.Stop();
            }
            else if (_output != null && Device.DeviceState == DeviceState.Active)
            {
                _output.Initialize(_volumeSource.ToWaveSource());
                _output.Play();
            }

            _channelCount = format.Channels;
        }
    }
}