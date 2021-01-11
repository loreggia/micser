using System;
using System.Runtime.InteropServices;

#pragma warning disable 649

namespace Micser.Common.Audio
{
    /// <inheritdoc cref="IWaveBuffer"/>
    [StructLayout(LayoutKind.Explicit, Pack = 2)]
    public class WaveBuffer : IWaveBuffer
    {
        [FieldOffset(0)]
        private int _byteCount;

        [FieldOffset(8)]
        private byte[] _byteBuffer = null!;

        [FieldOffset(8)]
        private readonly float[]? _floatBuffer;

        [FieldOffset(8)]
        private readonly short[]? _shortBuffer;

        [FieldOffset(8)]
        private readonly int[]? _intBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveBuffer"/> class.
        /// </summary>
        /// <param name="sizeToAllocateInBytes">The number of bytes. The size of the final buffer will be aligned on 4 Bytes (upper bound)</param>
        public WaveBuffer(int sizeToAllocateInBytes)
        {
            var aligned4Bytes = sizeToAllocateInBytes % 4;
            sizeToAllocateInBytes = (aligned4Bytes == 0) ? sizeToAllocateInBytes : sizeToAllocateInBytes + 4 - aligned4Bytes;
            // Allocating the byteBuffer is co-allocating the floatBuffer and the intBuffer
            _byteBuffer = new byte[sizeToAllocateInBytes];
            _byteCount = sizeToAllocateInBytes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveBuffer"/> class bound to a specific byte buffer.
        /// </summary>
        /// <param name="bufferToBindTo">A byte buffer to bind the WaveBuffer to.</param>
        public WaveBuffer(byte[] bufferToBindTo)
        {
            BindTo(bufferToBindTo);
        }

        /// <summary>
        /// Binds this WaveBuffer instance to a specific byte buffer.
        /// </summary>
        /// <param name="bufferToBoundTo">A byte buffer to bound the WaveBuffer to.</param>
        public void BindTo(byte[] bufferToBoundTo)
        {
            /* WaveBuffer assumes the caller knows what they are doing. We will let this pass
             * if ( (bufferToBoundTo.Length % 4) != 0 )
            {
                throw new ArgumentException("The byte buffer to bound must be 4 bytes aligned");
            }*/
            _byteBuffer = bufferToBoundTo;
            _byteCount = bufferToBoundTo.Length;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="WaveBuffer"/> to <see cref="System.Byte"/>.
        /// </summary>
        /// <param name="waveBuffer">The wave buffer.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator byte[](WaveBuffer waveBuffer)
        {
            return waveBuffer._byteBuffer;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="WaveBuffer"/> to <see cref="System.Single"/>.
        /// </summary>
        /// <param name="waveBuffer">The wave buffer.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator float[]?(WaveBuffer waveBuffer)
        {
            return waveBuffer._floatBuffer;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="WaveBuffer"/> to <see cref="System.Int32"/>.
        /// </summary>
        /// <param name="waveBuffer">The wave buffer.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator int[]?(WaveBuffer waveBuffer)
        {
            return waveBuffer._intBuffer;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="WaveBuffer"/> to <see cref="System.Int16"/>.
        /// </summary>
        /// <param name="waveBuffer">The wave buffer.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator short[]?(WaveBuffer waveBuffer)
        {
            return waveBuffer._shortBuffer;
        }

        /// <summary>
        /// Gets the byte buffer.
        /// </summary>
        /// <value>The byte buffer.</value>
        public byte[] ByteBuffer => _byteBuffer;

        /// <summary>
        /// Gets the float buffer.
        /// </summary>
        /// <value>The float buffer.</value>
        public float[]? FloatBuffer => _floatBuffer;

        /// <summary>
        /// Gets the short buffer.
        /// </summary>
        /// <value>The short buffer.</value>
        public short[]? ShortBuffer => _shortBuffer;

        /// <summary>
        /// Gets the int buffer.
        /// </summary>
        /// <value>The int buffer.</value>
        public int[]? IntBuffer => _intBuffer;

        /// <summary>
        /// Gets the max size in bytes of the byte buffer..
        /// </summary>
        /// <value>Maximum number of bytes in the buffer.</value>
        public int MaxSize => _byteBuffer.Length;

        /// <summary>
        /// Gets or sets the byte buffer count.
        /// </summary>
        /// <value>The byte buffer count.</value>
        public int ByteBufferCount
        {
            get => _byteCount;
            set => _byteCount = CheckValidityCount("ByteBufferCount", value, 1);
        }

        /// <summary>
        /// Gets or sets the float buffer count.
        /// </summary>
        /// <value>The float buffer count.</value>
        public int FloatBufferCount
        {
            get => _byteCount / 4;
            set => _byteCount = CheckValidityCount("FloatBufferCount", value, 4);
        }

        /// <summary>
        /// Gets or sets the short buffer count.
        /// </summary>
        /// <value>The short buffer count.</value>
        public int ShortBufferCount
        {
            get => _byteCount / 2;
            set => _byteCount = CheckValidityCount("ShortBufferCount", value, 2);
        }

        /// <summary>
        /// Gets or sets the int buffer count.
        /// </summary>
        /// <value>The int buffer count.</value>
        public int IntBufferCount
        {
            get => _byteCount / 4;
            set => _byteCount = CheckValidityCount("IntBufferCount", value, 4);
        }

        /// <summary>
        /// Clears the associated buffer.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_byteBuffer, 0, _byteBuffer.Length);
        }

        /// <summary>
        /// Copy this WaveBuffer to a destination buffer up to ByteBufferCount bytes.
        /// </summary>
        public void Copy(Array destinationArray)
        {
            Array.Copy(_byteBuffer, destinationArray, _byteCount);
        }

        /// <summary>
        /// Checks the validity of the count parameters.
        /// </summary>
        /// <param name="argName">Name of the arg.</param>
        /// <param name="value">The value.</param>
        /// <param name="sizeOfValue">The size of value.</param>
        private int CheckValidityCount(string argName, int value, int sizeOfValue)
        {
            var newNumberOfBytes = value * sizeOfValue;
            if ((newNumberOfBytes % 4) != 0)
            {
                throw new ArgumentOutOfRangeException(argName, string.Format("{0} cannot set a count ({1}) that is not 4 bytes aligned ", argName, newNumberOfBytes));
            }

            if (value < 0 || value > (_byteBuffer.Length / sizeOfValue))
            {
                throw new ArgumentOutOfRangeException(argName, string.Format("{0} cannot set a count that exceed max count {1}", argName, _byteBuffer.Length / sizeOfValue));
            }
            return newNumberOfBytes;
        }
    }
}