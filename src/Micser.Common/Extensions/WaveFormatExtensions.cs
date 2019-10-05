using CSCore;
using System;

namespace Micser.Common.Extensions
{
    /// <summary>
    /// Provides helper extension methods for the <see cref="WaveFormat"/> class.
    /// Copied from CSCore source, because these methods are declared internal in CSCore.
    /// </summary>
    public static class WaveFormatExtensions
    {
        /// <summary>
        /// Checks if the wave format is an IEEE float audio format.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsIeeeFloat(this WaveFormat waveFormat)
        {
            switch (waveFormat)
            {
                case null:
                    throw new ArgumentNullException(nameof(waveFormat));

                case WaveFormatExtensible waveFormatExtensible:
                    return waveFormatExtensible.SubFormat == AudioSubTypes.IeeeFloat;

                default:
                    return waveFormat.WaveFormatTag == AudioEncoding.IeeeFloat;
            }
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Checks if the wave format is a PCM audio format.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsPCM(this WaveFormat waveFormat)
        {
            switch (waveFormat)
            {
                case null:
                    throw new ArgumentNullException(nameof(waveFormat));

                case WaveFormatExtensible waveFormatExtensible:
                    return waveFormatExtensible.SubFormat == AudioSubTypes.Pcm;

                default:
                    return waveFormat.WaveFormatTag == AudioEncoding.Pcm;
            }
        }
    }
}