using CSCore;
using System;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.Audio
{
    /// <summary>
    /// A sample source that mixes multiple inputs together.
    /// </summary>
    public class MixerSampleSource : ISampleSource
    {
        private readonly object _lockObj = new object();
        private readonly List<ISampleSource> _sampleSources = new List<ISampleSource>();
        private float[] _mixerBuffer;

        public MixerSampleSource(int channelCount, int sampleRate)
        {
            if (channelCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(channelCount));
            }

            if (sampleRate < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            WaveFormat = new WaveFormat(sampleRate, 32, channelCount, AudioEncoding.IeeeFloat);
            FillWithZeros = false;
        }

        /// <summary>
        /// This is always false.
        /// </summary>
        public bool CanSeek => false;

        /// <summary>
        /// Gets or sets a value whether the resulting sample values are divided by the number of sources.
        /// </summary>
        public bool DivideResult { get; set; }

        /// <summary>
        /// Gets or sets a value whether the output is filled with zeroes if no input is available. Prevents stopping of playback.
        /// </summary>
        public bool FillWithZeros { get; set; }

        /// <summary>
        /// This is always 0.
        /// </summary>
        public long Length => 0;

        /// <summary>
        /// Not implemented.
        /// </summary>
        public long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the common wave format.
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Adds a new source to the mixer. The source's number of channels and sample rate have to be equal to the mixer's.
        /// </summary>
        public void AddSource(ISampleSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (source.WaveFormat.Channels != WaveFormat.Channels ||
               source.WaveFormat.SampleRate != WaveFormat.SampleRate)
            {
                throw new ArgumentException("Invalid format.", "source");
            }

            lock (_lockObj)
            {
                if (!Contains(source))
                {
                    _sampleSources.Add(source);
                }
            }
        }

        public bool Contains(ISampleSource source)
        {
            if (source == null)
            {
                return false;
            }

            return _sampleSources.Contains(source);
        }

        public void Dispose()
        {
            lock (_lockObj)
            {
                _sampleSources.Clear();
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var numberOfStoredSamples = 0;

            if (count > 0 && _sampleSources.Count > 0)
            {
                lock (_lockObj)
                {
                    _mixerBuffer = _mixerBuffer.CheckBuffer(count);
                    var numberOfReadSamples = new List<int>();
                    for (var m = _sampleSources.Count - 1; m >= 0; m--)
                    {
                        var sampleSource = _sampleSources[m];
                        var read = sampleSource.Read(_mixerBuffer, 0, count);
                        for (int i = offset, n = 0; n < read; i++, n++)
                        {
                            if (numberOfStoredSamples <= i)
                            {
                                buffer[i] = _mixerBuffer[n];
                            }
                            else
                            {
                                buffer[i] += _mixerBuffer[n];
                            }
                        }
                        if (read > numberOfStoredSamples)
                        {
                            numberOfStoredSamples = read;
                        }

                        if (read > 0)
                        {
                            numberOfReadSamples.Add(read);
                        }
                        else
                        {
                            //raise event here
                            RemoveSource(sampleSource); //remove the input to make sure that the event gets only raised once.
                        }
                    }

                    if (DivideResult)
                    {
                        numberOfReadSamples.Sort();
                        var currentOffset = offset;
                        var remainingSources = numberOfReadSamples.Count;

                        foreach (var readSamples in numberOfReadSamples)
                        {
                            if (remainingSources == 0)
                            {
                                break;
                            }

                            while (currentOffset < offset + readSamples)
                            {
                                buffer[currentOffset] /= remainingSources;
                                buffer[currentOffset] = Math.Max(-1, Math.Min(1, buffer[currentOffset]));
                                currentOffset++;
                            }
                            remainingSources--;
                        }
                    }
                }
            }

            if (FillWithZeros && numberOfStoredSamples != count)
            {
                Array.Clear(
                    buffer,
                    Math.Max(offset + numberOfStoredSamples - 1, 0),
                    count - numberOfStoredSamples);

                return count;
            }

            return numberOfStoredSamples;
        }

        public void RemoveSource(ISampleSource source)
        {
            //don't throw null ex here
            lock (_lockObj)
            {
                if (Contains(source))
                {
                    _sampleSources.Remove(source);
                }
            }
        }
    }
}