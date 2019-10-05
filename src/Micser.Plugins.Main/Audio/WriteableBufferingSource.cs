using CSCore;
using CSCore.Utils.Buffer;
using System;

namespace Micser.Plugins.Main.Audio
{
    /// <summary>
    /// Buffered WaveSource which overrides the allocated memory after the internal buffer got full.
    /// </summary>
    /// <remarks>
    /// Copied from CSCore & adjusted.
    /// </remarks>
    public class WriteableBufferingSource : IWaveSource
    {
        private readonly object _bufferlock = new object();
        private FixedSizeBuffer<byte> _buffer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:CSCore.Streams.WriteableBufferingSource" /> class.
        /// </summary>
        /// <param name="waveFormat">The WaveFormat of the source.</param>
        public WriteableBufferingSource(WaveFormat waveFormat)
        {
            WaveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));
        }

        /// <summary>
        /// Default destructor which calls <see cref="M:CSCore.Streams.WriteableBufferingSource.Dispose(System.Boolean)" />.
        /// </summary>
        ~WriteableBufferingSource()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:CSCore.IAudioSource" /> supports seeking.
        /// </summary>
        public bool CanSeek => false;

        /// <summary>
        /// Gets the number of stored bytes inside of the internal buffer.
        /// </summary>
        public long Length => _buffer?.Buffered ?? 0;

        /// <summary>Not supported.</summary>
        public long Position
        {
            get => 0;
            set => throw new InvalidOperationException();
        }

        /// <summary>
        /// Gets the <see cref="P:CSCore.IAudioSource.WaveFormat" /> of the waveform-audio data.
        /// </summary>
        public WaveFormat WaveFormat { get; }

        public void Clear()
        {
            lock (_bufferlock)
            {
                _buffer.Clear();
            }
        }

        /// <summary>
        /// Disposes the <see cref="T:CSCore.Streams.WriteableBufferingSource" /> and its internal buffer.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Reads a sequence of bytes from the internal buffer of the <see cref="T:CSCore.Streams.WriteableBufferingSource" /> and advances the position within the internal buffer by the number of bytes read.
        /// </summary>
        /// <param name="buffer">
        ///     An array of bytes. When this method returns, the <paramref name="buffer" /> contains the specified
        ///     byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> +
        ///     <paramref name="count" /> - 1) replaced by the bytes read from the internal buffer.
        /// </param>
        /// <param name="offset">
        ///     The zero-based byte offset in the <paramref name="buffer" /> at which to begin storing the data
        ///     read from the internal buffer.
        /// </param>
        /// <param name="count">The maximum number of bytes to read from the internal buffer.</param>
        /// <returns>The total number of bytes read into the <paramref name="buffer" />.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            lock (_bufferlock)
            {
                EnsureBuffer(count);

                var num = _buffer.Read(buffer, offset, count);

                if (num < count)
                {
                    Array.Clear(buffer, offset + num, count - num);
                }

                return count;
            }
        }

        /// <summary>Adds new data to the internal buffer.</summary>
        /// <param name="buffer">A byte-array which contains the data.</param>
        /// <param name="offset">Zero-based offset in the <paramref name="buffer" /> (specified in bytes).</param>
        /// <param name="count">Number of bytes to add to the internal buffer.</param>
        /// <returns>Number of added bytes.</returns>
        public int Write(byte[] buffer, int offset, int count)
        {
            lock (_bufferlock)
            {
                EnsureBuffer(count);

                return _buffer.Write(buffer, offset, count);
            }
        }

        /// <summary>
        /// Disposes the <see cref="T:CSCore.Streams.WriteableBufferingSource" /> and its internal buffer.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _buffer != null)
            {
                lock (_bufferlock)
                {
                    _buffer.Dispose();
                    _buffer = null;
                }
            }
        }

        private void EnsureBuffer(int count)
        {
            count *= 2;
            count += count % WaveFormat.BlockAlign;

            if (_buffer == null || _buffer.Length < count)
            {
                _buffer = new FixedSizeBuffer<byte>(count);
            }
        }
    }
}