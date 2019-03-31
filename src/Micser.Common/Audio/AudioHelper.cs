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

        public static float CompressorCurve(float db, float slope, float threshold, float knee)
        {
            if (slope > 0)
            {
                return CompressorCurveDown(db, slope, threshold, knee);
            }

            return CompressorCurveUp(db, -slope, threshold, knee);
        }

        public static float CompressorCurveDown(float db, float slope, float threshold, float knee)
        {
            // no knee in curve
            if (knee <= 0f)
            {
                return db < threshold ? db : threshold + slope * (db - threshold);
            }

            // below knee
            if (db - threshold < -knee / 2f)
            {
                return db;
            }

            // knee
            if (Math.Abs(db - threshold) <= knee / 2f)
            {
                return KneeCurveDown(db, slope, threshold, knee);
            }

            return threshold + slope * (db - threshold);
        }

        public static float CompressorCurveUp(float db, float slope, float threshold, float knee)
        {
            // no knee in curve
            if (knee <= 0f)
            {
                return db > threshold ? db : threshold + slope * (db - threshold);
            }

            // below knee
            if (db - threshold > knee / 2f)
            {
                return db;
            }

            // knee
            if (Math.Abs(db - threshold) <= knee / 2f)
            {
                return KneeCurveUp(db, slope, threshold, knee);
            }

            return threshold + slope * (db - threshold);
        }

        public static float DbToLinear(float value)
        {
            return (float)Math.Pow(10d, 0.05d * value);
        }

        /// <summary>
        /// Calculates a soft knee curve.
        /// </summary>
        /// <remarks>
        /// see doc/compressor-curve.html
        /// </remarks>
        public static float KneeCurve(float db, float slope, float threshold, float knee)
        {
            if (slope > 0)
            {
                return CompressorCurveDown(db, slope, threshold, knee);
            }

            return CompressorCurveUp(db, -slope, threshold, knee);
        }

        public static float KneeCurveDown(float db, float slope, float threshold, float knee)
        {
            var a = db - threshold + knee / 2f;
            return db + (slope - 1f) * a * a / (2f * knee);
        }

        public static float KneeCurveUp(float db, float slope, float threshold, float knee)
        {
            var a = -db + threshold + knee / 2f;
            return db - (slope - 1f) * a * a / (2f * knee);
        }

        public static float LinearToDb(float value)
        {
            return (float)(20d * Math.Log10(value));
        }
    }
}