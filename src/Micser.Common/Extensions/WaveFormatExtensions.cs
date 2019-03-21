using CSCore;
using System;

namespace Micser.Common.Extensions
{
    /// <summary>
    /// Copied from CSCore source, because these methods are declared internal in CSCore.
    /// </summary>
    public static class WaveFormatExtensions
    {
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