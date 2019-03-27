using CSCore;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Infrastructure.Audio
{
    public abstract class AudioModule : IAudioModule
    {
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        protected readonly float Epsilon = float.Epsilon;
        private readonly IList<IAudioModule> _outputs;

        private readonly IList<ISampleProcessor> _sampleProcessors;
        private float _volume = 1f;
        private IWaveBuffer _waveBuffer;

        protected AudioModule(long id)
        {
            Id = id;
            _outputs = new List<IAudioModule>(2);
            _sampleProcessors = new List<ISampleProcessor>
            {
                new VolumeSampleProcessor(this)
            };
        }

        ~AudioModule()
        {
            Dispose(false);
        }

        public virtual long Id { get; }

        public virtual bool IsMuted { get; set; }

        public virtual bool UseSystemVolume { get; set; }

        public virtual float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                MathExtensions.Clamp(ref _volume, 0f, 1f);
            }
        }

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

        public virtual ModuleState GetState()
        {
            var state = new ModuleState
            {
                IsMuted = IsMuted,
                Volume = Volume,
                UseSystemVolume = UseSystemVolume
            };

            this.GetStateProperties(state);

            return state;
        }

        public virtual void RemoveOutput(IAudioModule module)
        {
            if (_outputs.Contains(module))
            {
                _outputs.Remove(module);
            }
        }

        public virtual void SetState(ModuleState state)
        {
            if (state == null)
            {
                return;
            }

            IsMuted = state.IsMuted;
            Volume = state.Volume;
            UseSystemVolume = state.UseSystemVolume;

            this.SetStateProperties(state);
        }

        public virtual void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count)
        {
            if (IsMuted || Math.Abs(Volume) < Epsilon)
            {
                return;
            }

            byte[] nextBuffer;
            int nextOffset;

            if (Math.Abs(Volume - 1f) < Epsilon && _sampleProcessors.All(p => p is VolumeSampleProcessor))
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

                if (waveFormat.IsPCM())
                {
                    switch (waveFormat.BytesPerSample)
                    {
                        case 1:
                            for (var i8 = 0; i8 < _waveBuffer.ByteBufferCount; i8++)
                            {
                                var fSample8 = _waveBuffer.ByteBuffer[i8] / 256f;
                                foreach (var sampleProcessor in sampleProcessors)
                                {
                                    sampleProcessor.Process(waveFormat, ref fSample8);
                                }
                                _waveBuffer.ByteBuffer[i8] = (byte)(fSample8 * 255f);
                            }

                            nextBuffer = _waveBuffer.ByteBuffer;
                            break;

                        case 2:
                            for (var i16 = 0; i16 < _waveBuffer.ShortBufferCount; i16++)
                            {
                                var fSample16 = _waveBuffer.ShortBuffer[i16] / 32767f;
                                foreach (var sampleProcessor in sampleProcessors)
                                {
                                    sampleProcessor.Process(waveFormat, ref fSample16);
                                }
                                _waveBuffer.ShortBuffer[i16] = (short)(fSample16 * 32768f);
                            }

                            nextBuffer = _waveBuffer.ByteBuffer;
                            break;

                        case 3:
                            nextBuffer = new byte[count];
                            for (var i24 = 0; i24 < count / 3; i24 += 3)
                            {
                                var fSample24 = (((sbyte)buffer[offset + i24 + 2] << 16) | (buffer[offset + i24 + 1] << 8) | buffer[offset + i24]) / 8388608f;
                                foreach (var sampleProcessor in sampleProcessors)
                                {
                                    sampleProcessor.Process(waveFormat, ref fSample24);
                                }
                                var sample24 = (int)(fSample24 * 8388607f);
                                nextBuffer[i24] = (byte)(sample24);
                                nextBuffer[i24 + 1] = (byte)(sample24 >> 8);
                                nextBuffer[i24 + 2] = (byte)(sample24 >> 16);
                            }

                            break;

                        case 4:
                            for (var i32 = 0; i32 < _waveBuffer.IntBufferCount; i32++)
                            {
                                var fSample32 = _waveBuffer.IntBuffer[i32] / 2147483648f;
                                foreach (var sampleProcessor in sampleProcessors)
                                {
                                    sampleProcessor.Process(waveFormat, ref fSample32);
                                }
                                _waveBuffer.IntBuffer[i32] = (int)(fSample32 * 2147483647f);
                            }

                            nextBuffer = _waveBuffer.ByteBuffer;
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
                            for (var iF = 0; iF < _waveBuffer.FloatBufferCount; iF++)
                            {
                                var fSample = _waveBuffer.FloatBuffer[iF];
                                foreach (var sampleProcessor in sampleProcessors)
                                {
                                    sampleProcessor.Process(waveFormat, ref fSample);
                                }
                                _waveBuffer.FloatBuffer[iF] = fSample;
                            }

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

            foreach (var module in _outputs)
            {
                module.Write(source, waveFormat, nextBuffer, nextOffset, count);
            }
        }

        protected void AddSampleProcessor(ISampleProcessor sampleProcessor)
        {
            if (!_sampleProcessors.Contains(sampleProcessor))
            {
                _sampleProcessors.Add(sampleProcessor);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _outputs.Clear();
            }
        }

        protected void RemoveSampleProcessor<T>()
        {
            var processor = _sampleProcessors.FirstOrDefault(p => p is T);
            RemoveSampleProcessor(processor);
        }

        protected void RemoveSampleProcessor(ISampleProcessor sampleProcessor)
        {
            if (_sampleProcessors.Contains(sampleProcessor))
            {
                _sampleProcessors.Remove(sampleProcessor);
            }
        }
    }
}