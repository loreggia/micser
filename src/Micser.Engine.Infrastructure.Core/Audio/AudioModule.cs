using CSCore;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Infrastructure.Audio
{
    /// <summary>
    /// Base implementation of an audio module. Handles sample processors and state management.
    /// </summary>
    public abstract class AudioModule : IAudioModule
    {
        /// <summary>
        /// A small epsilon value for float equality comparison.
        /// </summary>
        public static readonly float Epsilon = float.Epsilon;

        /// <summary>
        /// The logger for the current class.
        /// </summary>
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly IList<IAudioModule> _outputs;

        private readonly IList<ISampleProcessor> _sampleProcessors;
        private float[] _channelSamplesBuffer;
        private float _volume = 1f;
        private IWaveBuffer _waveBuffer;

        /// <inheritdoc />
        protected AudioModule()
        {
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

        /// <inheritdoc />
        public virtual bool UseSystemVolume { get; set; }

        /// <inheritdoc />
        public virtual float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                MathExtensions.Clamp(ref _volume, 0f, 1f);
            }
        }

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
            if (state == null)
            {
                return;
            }

            IsEnabled = state.IsEnabled;
            IsMuted = state.IsMuted;
            Volume = state.Volume;
            UseSystemVolume = state.UseSystemVolume;

            this.SetStateProperties(state);
        }

        /// <inheritdoc />
        public virtual void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count)
        {
            if (IsEnabled && (IsMuted || Math.Abs(Volume) < Epsilon))
            {
                return;
            }

            byte[] nextBuffer = null;
            int nextOffset;

            if (!IsEnabled || Math.Abs(Volume - 1f) < Epsilon && _sampleProcessors.All(p => p is VolumeSampleProcessor))
            {
                nextBuffer = buffer;
                nextOffset = offset;
            }
            else
            {
                var sampleProcessors = _sampleProcessors.Where(p => p.IsEnabled).OrderByDescending(p => p.Priority).ToArray();

                nextOffset = 0;

                // when adjusting volume we need to make a copy of the buffer
                if (waveFormat.BytesPerSample != 3 && (_waveBuffer == null || _waveBuffer.MaxSize < count))
                {
                    _waveBuffer = new WaveBuffer(count);
                }
                Array.Copy(buffer, offset, _waveBuffer.ByteBuffer, 0, count);

                if (_channelSamplesBuffer == null || _channelSamplesBuffer.Length != waveFormat.Channels)
                {
                    _channelSamplesBuffer = new float[waveFormat.Channels];
                }

                if (_waveBuffer != null)
                {
                    nextBuffer = _waveBuffer.ByteBuffer;
                }

                if (waveFormat.IsPCM())
                {
                    switch (waveFormat.BytesPerSample)
                    {
                        case 1:
                            ProcessPcm8(waveFormat, sampleProcessors, count);
                            break;

                        case 2:
                            ProcessPcm16(waveFormat, sampleProcessors, count);
                            break;

                        case 3:
                            nextBuffer = new byte[count];
                            for (var i = 0; i < count / 3; i += 3 * waveFormat.Channels)
                            {
                                for (int c = 0; c < waveFormat.Channels; c++)
                                {
                                    var cOffset = i + (3 * c);
                                    var fSample24 = (((sbyte)buffer[offset + cOffset + 2] << 16) | (buffer[offset + cOffset + 1] << 8) | buffer[offset + cOffset]) / 8388608f;
                                    _channelSamplesBuffer[c] = fSample24;
                                }

                                foreach (var sampleProcessor in sampleProcessors)
                                {
                                    sampleProcessor.Process(waveFormat, _channelSamplesBuffer);
                                }

                                for (int c = 0; c < waveFormat.Channels; c++)
                                {
                                    var cOffset = i + (3 * c);
                                    var fSample24 = _channelSamplesBuffer[c];
                                    var sample24 = (int)(fSample24 * 8388607f);
                                    nextBuffer[cOffset] = (byte)(sample24);
                                    nextBuffer[cOffset + 1] = (byte)(sample24 >> 8);
                                    nextBuffer[cOffset + 2] = (byte)(sample24 >> 16);
                                }
                            }

                            break;

                        case 4:
                            ProcessPcm32(waveFormat, sampleProcessors, count);
                            break;

                        default:
                            Logger.Error($"Unsupported bitrate: {waveFormat.BitsPerSample} (only 8/16/24/32bit PCM audio is supported.");
                            return;
                    }
                }
                else if (waveFormat.IsIeeeFloat())
                {
                    switch (waveFormat.BytesPerSample)
                    {
                        case 4:
                            ProcessIeeeFloat(waveFormat, sampleProcessors, count);
                            nextBuffer = _waveBuffer.ByteBuffer;
                            break;

                        default:
                            Logger.Error("Only 32bit IEEE float audio is supported.");
                            return;
                    }
                }
                else
                {
                    Logger.Error($"Unsupported audio format '{waveFormat}'. Only PCM or IEEE audio is supported.");
                    return;
                }
            }

            if (nextBuffer == null)
            {
                Logger.Error("No buffer set to pass to output modules.");
                return;
            }

            foreach (var module in _outputs.ToArray())
            {
                module.Write(source, waveFormat, nextBuffer, nextOffset, count);
            }
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _outputs?.Clear();
            }
        }

        private void ProcessIeeeFloat(WaveFormat waveFormat, ISampleProcessor[] sampleProcessors, int byteCount)
        {
            for (var i = 0; i < byteCount / 4; i += waveFormat.Channels)
            {
                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _channelSamplesBuffer[c] = _waveBuffer.FloatBuffer[i + c];
                }

                foreach (var sampleProcessor in sampleProcessors)
                {
                    sampleProcessor.Process(waveFormat, _channelSamplesBuffer);
                }

                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _waveBuffer.FloatBuffer[i + c] = _channelSamplesBuffer[c];
                }
            }
        }

        private void ProcessPcm16(WaveFormat waveFormat, ISampleProcessor[] sampleProcessors, int byteCount)
        {
            for (var i = 0; i < byteCount / 2; i += waveFormat.Channels)
            {
                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _channelSamplesBuffer[c] = _waveBuffer.ShortBuffer[i + c] / 32767f;
                }

                foreach (var sampleProcessor in sampleProcessors)
                {
                    sampleProcessor.Process(waveFormat, _channelSamplesBuffer);
                }

                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _waveBuffer.ShortBuffer[i + c] = (short)(_channelSamplesBuffer[c] * 32768f);
                }
            }
        }

        private void ProcessPcm32(WaveFormat waveFormat, ISampleProcessor[] sampleProcessors, int byteCount)
        {
            for (var i = 0; i < byteCount / 4; i += waveFormat.Channels)
            {
                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _channelSamplesBuffer[c] = _waveBuffer.IntBuffer[i + c] / 2147483648f;
                }

                foreach (var sampleProcessor in sampleProcessors)
                {
                    sampleProcessor.Process(waveFormat, _channelSamplesBuffer);
                }

                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _waveBuffer.IntBuffer[i + c] = (int)(_channelSamplesBuffer[c] * 2147483647f);
                }
            }
        }

        private void ProcessPcm8(WaveFormat waveFormat, ISampleProcessor[] sampleProcessors, int byteCount)
        {
            for (var i = 0; i < byteCount; i += waveFormat.Channels)
            {
                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _channelSamplesBuffer[c] = _waveBuffer.ByteBuffer[i + c] / 256f;
                }

                foreach (var sampleProcessor in sampleProcessors)
                {
                    sampleProcessor.Process(waveFormat, _channelSamplesBuffer);
                }

                for (int c = 0; c < waveFormat.Channels; c++)
                {
                    _waveBuffer.ByteBuffer[i + c] = (byte)(_channelSamplesBuffer[c] * 255f);
                }
            }
        }
    }
}