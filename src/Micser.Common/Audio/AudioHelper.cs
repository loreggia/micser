using System;

namespace Micser.Common.Audio
{
    public static class AudioHelper
    {
        /// <summary>
        /// Calculates the adaptive release curve using the 4 coefficients a-d. Formula: a*x^3 + b*x^2 + c*x + d
        /// </summary>
        /// <remarks>
        /// for more information on the adaptive release curve, check out adaptive-release-curve.html demo + source code included in this repo -> https://github.com/voidqk/sndfilter
        /// </remarks>
        public static float AdaptiveReleaseCurve(float x, float a, float b, float c, float d)
        {
            var x2 = x * x;
            return a * x2 * x + b * x2 + c * x + d;
        }

        public static float CompCurve(float x, float k, float slope, float linearThreshold, float linearThresholdKnee, float threshold, float knee, float kneeDbOffset)
        {
            if (x < linearThreshold)
            {
                return x;
            }

            if (knee <= 0f)
            {
                // no knee in curve
                return DbToLinear(threshold + slope * (LinearToDb(x) - threshold));
            }

            if (x < linearThresholdKnee)
            {
                return KneeCurve(x, k, linearThreshold);
            }

            return DbToLinear(kneeDbOffset + slope * (LinearToDb(x) - threshold - knee));
        }

        public static float DbToLinear(float value)
        {
            return (float)Math.Pow(10d, 0.05d * value);
        }

        /// <summary>
        /// Calculates a soft knee curve.
        /// </summary>
        /// <remarks>
        /// for more information on the knee curve, check out the compressor-curve.html demo + source code included in this repo -> https://github.com/voidqk/sndfilter
        /// </remarks>
        public static float KneeCurve(float x, float k, float linearThreshold)
        {
            return linearThreshold + (1.0f - (float)Math.Exp(-k * (x - linearThreshold))) / k;
        }

        public static float KneeSlope(float x, float k, float linearThreshold)
        {
            return k * x / ((k * linearThreshold + 1.0f) * (float)Math.Exp(k * (x - linearThreshold)) - 1);
        }

        public static float LinearToDb(float value)
        {
            return (float)(20d * Math.Log10(value));
        }
    }
}