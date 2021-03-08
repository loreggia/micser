using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CSCore;
using Microsoft.Extensions.Logging;
using Micser.Common.Extensions;
using Micser.Common.Modules;

namespace Micser.Common.Audio
{
    /// <summary>
    /// Base implementation of an audio module. Handles sample processors and state management.
    /// </summary>
    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach", Justification = "Performance")]
    public abstract class AudioModule : IAudioModule
    {
        private readonly ILogger _logger;
        private readonly List<IAudioModule> _outputs;
        private readonly List<ISampleProcessor> _sampleProcessors;
        private float[]? _channelSamplesBuffer;
        private float _volume = 1f;
        private IWaveBuffer? _waveBuffer;

        protected AudioModule(ILogger logger)
        {
            _logger = logger;
            _outputs = new List<IAudioModule>(2);
            _sampleProcessors = new List<ISampleProcessor>
            {
                new VolumeSampleProcessor(this)
            };
        }

        /// <inheritdoc />
        ~AudioModule()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public virtual long Id { get; set; }

        /// <inheritdoc />
        public bool IsEnabled { get; set; }

        /// <inheritdoc />
        public virtual bool IsMuted { get; set; }

        public IEnumerable<ISampleProcessor> SampleProcessors => _sampleProcessors.AsReadOnly();

        /// <inheritdoc />
        public virtual bool UseSystemVolume { get; set; }

        /// <inheritdoc />
        public virtual float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                _volume.Clamp(0f, 1f);
            }
        }

        protected internal float[]? ChannelSamplesBuffer => _channelSamplesBuffer;
        private bool IsMutedInternal => IsEnabled && (IsMuted || Math.Abs(Volume) < float.Epsilon);
        private bool IsPassThrough => !IsEnabled || Math.Abs(Volume - 1f) < float.Epsilon && _sampleProcessors.TrueForAll(p => p is VolumeSampleProcessor);

        /// <inheritdoc />
        public virtual void AddOutput(IAudioModule module)
        {
            if (_outputs.Contains(module))
            {
                return;
            }

            _outputs.Add(module);
        }

        /// <inheritdoc />
        public void AddSampleProcessor(ISampleProcessor sampleProcessor)
        {
            if (!_sampleProcessors.Contains(sampleProcessor))
            {
                _sampleProcessors.Add(sampleProcessor);
                // sort by descending priority
                _sampleProcessors.Sort((p1, p2) => p2.Priority.CompareTo(p1.Priority));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public virtual ModuleState GetState()
        {
            var state = new ModuleState
            {
                IsEnabled = IsEnabled,
                IsMuted = IsMuted,
                Volume = Volume,
                UseSystemVolume = UseSystemVolume
            };

            this.GetStateProperties(state);

            return state;
        }

        /// <inheritdoc />
        public virtual void RemoveOutput(IAudioModule module)
        {
            if (_outputs.Contains(module))
            {
                _outputs.Remove(module);
            }
        }

        /// <inheritdoc />
        public void RemoveSampleProcessor<T>()
            where T : ISampleProcessor
        {
            var processors = _sampleProcessors.Where(p => p is T).ToArray();
            foreach (var processor in processors)
            {
                RemoveSampleProcessor(processor);
            }
        }

        /// <inheritdoc />
        public void RemoveSampleProcessor(ISampleProcessor sampleProcessor)
        {
            if (_sampleProcessors.Contains(sampleProcessor))
            {
                _sampleProcessors.Remove(sampleProcessor);
            }
        }

        /// <inheritdoc />
        public virtual void SetState(ModuleState state)
        {
            IsEnabled = state.IsEnabled;
            IsMuted = state.IsMuted;
            Volume = state.Volume;
            UseSystemVolume = state.UseSystemVolume;

            this.SetStateProperties(state);
        }

        /// <inheritdoc />
        public virtual void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count)
        {
            if (IsMutedInternal)
            {
                return;
            }

            if (IsPassThrough)
            {
                for (var i = 0; i < _outputs.Count; i++)
                {
                    _outputs[i].Write(source, waveFormat, buffer, offset, count);
                }
            }
            else
            {
                if (waveFormat.BytesPerSample != 3 && (_waveBuffer == null || _waveBuffer.MaxSize < count))
                {
                    _waveBuffer = new WaveBuffer(count);
                }

                if (_waveBuffer != null)
                {
                    Array.Copy(buffer, offset, _waveBuffer.ByteBuffer, 0, count);
                }

                if (_channelSamplesBuffer == null || _channelSamplesBuffer.Length != waveFormat.Channels)
                {
                    _channelSamplesBuffer = new float[waveFormat.Channels];
                }

                if (waveFormat.IsPCM())
                {
                    switch (waveFormat.BytesPerSample)
                    {
                        case 1:
                            ProcessPcm8(waveFormat, count);
                            break;

                        case 2:
                            ProcessPcm16(waveFormat, count);
                            break;

                        case 3:
                            ProcessPcm24(waveFormat, buffer, offset, count);
                            break;

                        case 4:
                            ProcessPcm32(waveFormat, count);
                            break;

                        default:
                            _logger.LogError($"Unsupported bitrate: {waveFormat.BitsPerSample} (only 8/16/24/32bit PCM audio is supported.");
                            return;
                    }
                }
                else if (waveFormat.IsIeeeFloat())
                {
                    switch (waveFormat.BytesPerSample)
                    {
                        case 4:
                            ProcessIeeeFloat(waveFormat, count);
                            break;

                        default:
                            _logger.LogError("Only 32bit IEEE float audio is supported.");
                            return;
                    }
                }
                else
                {
                    _logger.LogError($"Unsupported audio format '{waveFormat}'. Only PCM or IEEE audio is supported.");
                }
            }
        }

        /// <inheritdoc />
        public virtual void WriteSample(IAudioModule source, WaveFormat waveFormat, float[] channelSamples)
        {
            if (IsMutedInternal)
            {
                return;
            }

            if (IsPassThrough)
            {
                for (var i = 0; i < _outputs.Count; i++)
                {
                    _outputs[i].WriteSample(this, waveFormat, channelSamples);
                }
            }
            else
            {
                if (_channelSamplesBuffer == null || _channelSamplesBuffer.Length != channelSamples.Length)
                {
                    _channelSamplesBuffer = new float[waveFormat.Channels];
                }

                // copy source data (the parameter data should not be changed).
                for (var i = 0; i < channelSamples.Length; i++)
                {
                    _channelSamplesBuffer[i] = channelSamples[i];
                }

                WriteSampleInternal(waveFormat, _channelSamplesBuffer);
            }
        }

        internal void WriteSampleInternal(WaveFormat waveFormat, float[] channelSamples)
        {
            for (var i = 0; i < _sampleProcessors.Count; i++)
            {
                if (_sampleProcessors[i].IsEnabled)
                {
                    _sampleProcessors[i].Process(waveFormat, channelSamples);
                }
            }

            for (var i = 0; i < _outputs.Count; i++)
            {
                _outputs[i].WriteSample(this, waveFormat, channelSamples);
            }
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _outputs.Clear();
            }
        }

        private void ProcessIeeeFloat(WaveFormat waveFormat, int byteCount)
        {
            if (_channelSamplesBuffer == null || _waveBuffer?.FloatBuffer == null)
            {
                return;
            }

            for (var i = 0; i < byteCount / 4; i += waveFormat.Channels)
            {
                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _channelSamplesBuffer[c] = _waveBuffer.FloatBuffer[i + c];
                }

                WriteSampleInternal(waveFormat, _channelSamplesBuffer);
            }
        }

        private void ProcessPcm16(WaveFormat waveFormat, int byteCount)
        {
            if (_channelSamplesBuffer == null || _waveBuffer?.ShortBuffer == null)
            {
                return;
            }

            for (var i = 0; i < byteCount / 2; i += waveFormat.Channels)
            {
                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _channelSamplesBuffer[c] = _waveBuffer.ShortBuffer[i + c] / 32767f;
                }

                WriteSampleInternal(waveFormat, _channelSamplesBuffer);
            }
        }

        private void ProcessPcm24(WaveFormat waveFormat, byte[] buffer, int offset, int count)
        {
            if (_channelSamplesBuffer == null)
            {
                return;
            }

            for (var i = 0; i < count / 3; i += 3 * waveFormat.Channels)
            {
                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    var cOffset = i + (3 * c);
                    var fSample24 = (((sbyte)buffer[offset + cOffset + 2] << 16) | (buffer[offset + cOffset + 1] << 8) | buffer[offset + cOffset]) / 8388608f;
                    _channelSamplesBuffer[c] = fSample24;
                }

                WriteSampleInternal(waveFormat, _channelSamplesBuffer);
            }
        }

        private void ProcessPcm32(WaveFormat waveFormat, int byteCount)
        {
            if (_channelSamplesBuffer == null || _waveBuffer?.IntBuffer == null)
            {
                return;
            }

            for (var i = 0; i < byteCount / 4; i += waveFormat.Channels)
            {
                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _channelSamplesBuffer[c] = _waveBuffer.IntBuffer[i + c] / 2147483648f;
                }

                WriteSampleInternal(waveFormat, _channelSamplesBuffer);
            }
        }

        private void ProcessPcm8(WaveFormat waveFormat, int byteCount)
        {
            if (_channelSamplesBuffer == null || _waveBuffer == null)
            {
                return;
            }

            for (var i = 0; i < byteCount; i += waveFormat.Channels)
            {
                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _channelSamplesBuffer[c] = _waveBuffer.ByteBuffer[i + c] / 256f;
                }

                WriteSampleInternal(waveFormat, _channelSamplesBuffer);
            }
        }
    }
}