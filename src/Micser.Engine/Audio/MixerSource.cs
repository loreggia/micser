using System;
using System.Collections.Generic;
using CSCore;

namespace Micser.Engine.Audio
{
    public class MixerSource : ISampleSource
    {
        private readonly object _lockObj = new object();
        private readonly List<ISampleSource> _sampleSources = new List<ISampleSource>();
        private float[] _mixerBuffer;

        public MixerSource(WaveFormat format)
        {
            WaveFormat = format ?? throw new ArgumentNullException(nameof(format));
            FillWithZeros = false;
        }

        public bool DivideResult { get; set; }
        public bool FillWithZeros { get; set; }

        public bool CanSeek => false;

        public long Length => 0;

        public long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public WaveFormat WaveFormat { get; }

        public void Dispose()
        {
            lock (_lockObj)
            {
                foreach (var sampleSource in _sampleSources.ToArray())
                {
                    sampleSource.Dispose();
                    _sampleSources.Remove(sampleSource);
                }
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

        public void AddSource(ISampleSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source.WaveFormat.Channels != WaveFormat.Channels ||
                source.WaveFormat.SampleRate != WaveFormat.SampleRate)
            {
                throw new ArgumentException("Invalid format.", nameof(source));
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