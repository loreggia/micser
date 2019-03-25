using CSCore;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Extensions;
using NLog;
using System;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.Audio
{
    public abstract class AudioModule : IAudioModule
    {
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        protected readonly float Epsilon = float.Epsilon;
        private readonly IList<IAudioModule> _outputs;

        private float _volume = 1f;
        private IWaveBuffer _waveBuffer;

        protected AudioModule(long id)
        {
            Id = id;
            _outputs = new List<IAudioModule>(2);
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
            return new ModuleState
            {
                IsMuted = IsMuted,
                Volume = Volume,
                UseSystemVolume = UseSystemVolume
            };
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
        }

        public virtual void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count)
        {
            if (IsMuted || Math.Abs(Volume) < Epsilon)
            {
                return;
            }

            byte[] nextBuffer;
            int nextOffset;

            if (Math.Abs(Volume - 1f) < Epsilon)
            {
                nextBuffer = buffer;
                nextOffset = offset;
            }
            else
            {
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
                            for (var iB = 0; iB < _waveBuffer.ByteBufferCount; iB++)
                            {
                                var volB = _waveBuffer.ByteBuffer[iB] * Volume;
                                //MathExtensions.Clamp(ref volB, byte.MinValue, byte.MaxValue);
                                _waveBuffer.ByteBuffer[iB] = (byte)volB;
                            }

                            nextBuffer = _waveBuffer.ByteBuffer;
                            break;

                        case 2:
                            for (var iS = 0; iS < _waveBuffer.ShortBufferCount; iS++)
                            {
                                var volS = _waveBuffer.ShortBuffer[iS] * Volume;
                                //MathExtensions.Clamp(ref volS, short.MinValue, short.MaxValue);
                                _waveBuffer.ShortBuffer[iS] = (short)volS;
                            }

                            nextBuffer = _waveBuffer.ByteBuffer;
                            break;

                        case 3:
                            nextBuffer = new byte[count];
                            for (var i24 = 0; i24 < count / 3; i24 += 3)
                            {
                                var vol24 = (((sbyte)buffer[offset + i24 + 2] << 16) | (buffer[offset + i24 + 1] << 8) | buffer[offset + i24]) /
                                            8388608f * Volume;
                                //MathExtensions.Clamp(ref vol24, -1f, 1f);
                                var sample24 = (int)(vol24 * 8388607.0);
                                nextBuffer[i24] = (byte)(sample24);
                                nextBuffer[i24 + 1] = (byte)(sample24 >> 8);
                                nextBuffer[i24 + 2] = (byte)(sample24 >> 16);
                            }

                            break;

                        case 4:
                            for (var iI = 0; iI < _waveBuffer.IntBufferCount; iI++)
                            {
                                var volI = _waveBuffer.IntBuffer[iI] * Volume;
                                //MathExtensions.Clamp(ref volI, int.MinValue, int.MaxValue);
                                _waveBuffer.IntBuffer[iI] = (int)volI;
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
                                var volF = _waveBuffer.FloatBuffer[iF];
                                volF *= Volume;
                                //MathExtensions.Clamp(ref volF, -1f, 1f);
                                _waveBuffer.FloatBuffer[iF] = volF;
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _outputs.Clear();
            }
        }
    }
}