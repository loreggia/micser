#region Copyright

/****************************************************************************
*
* NAME: PitchShift.cs
* VERSION: 1.0
* HOME URL: http://www.dspdimension.com
* KNOWN BUGS: none
*
* SYNOPSIS: Routine for doing pitch shifting while maintaining
* duration using the Short Time Fourier Transform.
*
* DESCRIPTION: The routine takes a pitchShift factor value which is between 0.5
* (one octave down) and 2. (one octave up). A value of exactly 1 does not change
* the pitch. numSampsToProcess tells the routine how many samples in indata[0...
* numSampsToProcess-1] should be pitch shifted and moved to outdata[0 ...
* numSampsToProcess-1]. The two buffers can be identical (ie. it can process the
* data in-place). fftFrameSize defines the FFT frame size used for the
* processing. Typical values are 1024, 2048 and 4096. It may be any value <=
* MAX_FRAME_LENGTH but it MUST be a power of 2. osamp is the STFT
* oversampling factor which also determines the overlap between adjacent STFT
* frames. It should at least be 4 for moderate scaling ratios. A value of 32 is
* recommended for best quality. sampleRate takes the sample rate for the signal
* in unit Hz, ie. 44100 for 44.1 kHz audio. The data passed to the routine in
* indata[] should be in the range [-1.0, 1.0), which is also the output range
* for the data, make sure you scale the data accordingly (for 16bit signed integers
* you would have to divide (and multiply) by 32768).
*
* COPYRIGHT 1999-2006 Stephan M. Bernsee <smb [AT] dspdimension [DOT] com>
*
* 						The Wide Open License (WOL)
*
* Permission to use, copy, modify, distribute and sell this software and its
* documentation for any purpose is hereby granted without fee, provided that
* the above copyright notice and this license appear in all source copies.
* THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED WARRANTY OF
* ANY KIND. See http://www.dspguru.com/wol.htm for more information.
*
*****************************************************************************/

/****************************************************************************
*
* This code was converted to C# by Michael Knight
* madmik3 at gmail dot com.
* http://sites.google.com/site/mikescoderama/
*
*****************************************************************************/

#endregion Copyright

using CSCore;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Audio
{
    public class PitchSampleProcessor : SampleProcessor
    {
        private const int MAX_FRAME_LENGTH = 4096;

        private readonly float[] _anaFreq = new float[MAX_FRAME_LENGTH];
        private readonly float[] _anaMagn = new float[MAX_FRAME_LENGTH];
        private readonly float[] _fftWorksp = new float[2 * MAX_FRAME_LENGTH];
        private readonly float[] _inFiFo = new float[MAX_FRAME_LENGTH];
        private readonly float[] _lastPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        private readonly PitchModule _module;
        private readonly float[] _outFiFo = new float[MAX_FRAME_LENGTH];
        private readonly float[] _outputAccum = new float[2 * MAX_FRAME_LENGTH];
        private readonly float[] _sumPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        private readonly float[] _synFreq = new float[MAX_FRAME_LENGTH];
        private readonly float[] _synMagn = new float[MAX_FRAME_LENGTH];

        private int _fftSize;
        private int _oversampling;
        private float _pitch;
        private long _rover;
        private int _sampleRate;

        public PitchSampleProcessor(PitchModule module)
        {
            _module = module;
        }

        public override void Process(WaveFormat waveFormat, float[] channelSamples)
        {
            if (Math.Abs(_pitch - _module.PitchFactor) > AudioModule.Epsilon ||
                _fftSize != _module.FftSize ||
                _oversampling != _module.Oversampling ||
                _sampleRate != waveFormat.SampleRate)
            {
                Init(waveFormat);
            }

            PitchShift(channelSamples);
        }

        private static void ShortTimeFourierTransform(float[] fftBuffer, long fftFrameSize, long sign)
        {
            for (var i = 2; i < 2 * fftFrameSize - 2; i += 2)
            {
                long bitm;
                long j;
                for (bitm = 2, j = 0; bitm < 2 * fftFrameSize; bitm <<= 1)
                {
                    if ((i & bitm) != 0) j++;
                    j <<= 1;
                }
                if (i < j)
                {
                    var temp = fftBuffer[i];
                    fftBuffer[i] = fftBuffer[j];
                    fftBuffer[j] = temp;
                    temp = fftBuffer[i + 1];
                    fftBuffer[i + 1] = fftBuffer[j + 1];
                    fftBuffer[j + 1] = temp;
                }
            }

            var max = (long)(Math.Log(fftFrameSize) / Math.Log(2.0) + 0.5);
            var le = 2;
            for (var k = 0; k < max; k++)
            {
                le <<= 1;
                var le2 = le >> 1;
                var ur = 1.0F;
                var ui = 0.0F;
                var arg = (float)Math.PI / (le2 >> 1);
                var wr = (float)Math.Cos(arg);
                var wi = (float)(sign * Math.Sin(arg));

                for (var j = 0; j < le2; j += 2)
                {
                    float tr;
                    for (var i = j; i < 2 * fftFrameSize; i += le)
                    {
                        tr = fftBuffer[i + le2] * ur - fftBuffer[i + le2 + 1] * ui;
                        var ti = fftBuffer[i + le2] * ui + fftBuffer[i + le2 + 1] * ur;
                        fftBuffer[i + le2] = fftBuffer[i] - tr;
                        fftBuffer[i + le2 + 1] = fftBuffer[i + 1] - ti;
                        fftBuffer[i] += tr;
                        fftBuffer[i + 1] += ti;
                    }
                    tr = ur * wr - ui * wi;
                    ui = ur * wi + ui * wr;
                    ur = tr;
                }
            }
        }

        private void Init(WaveFormat waveFormat)
        {
            _pitch = _module.PitchFactor;
            _fftSize = _module.FftSize;
            _oversampling = _module.Oversampling;
            _sampleRate = waveFormat.SampleRate;

            _rover = 0;
            Array.Clear(_anaFreq, 0, _anaFreq.Length);
            Array.Clear(_anaMagn, 0, _anaMagn.Length);
            Array.Clear(_fftWorksp, 0, _fftWorksp.Length);
            Array.Clear(_inFiFo, 0, _inFiFo.Length);
            Array.Clear(_lastPhase, 0, _lastPhase.Length);
            Array.Clear(_outFiFo, 0, _outFiFo.Length);
            Array.Clear(_outputAccum, 0, _outputAccum.Length);
            Array.Clear(_sumPhase, 0, _sumPhase.Length);
            Array.Clear(_synFreq, 0, _synFreq.Length);
            Array.Clear(_synMagn, 0, _synMagn.Length);
        }

        private void PitchShift(float[] data)
        {
            var sampleCount = data.Length;
            var inData = data;
            var outData = data;
            var osamp = _oversampling;
            var fftFrameSize = _fftSize;
            var sampleRate = _sampleRate;
            var pitchShift = _pitch;

            /* set up some handy variables */
            var fftFrameSize2 = fftFrameSize / 2;
            var stepSize = fftFrameSize / osamp;
            var freqPerBin = sampleRate / (double)fftFrameSize;
            var expct = 2.0 * Math.PI * stepSize / fftFrameSize;
            var inFifoLatency = fftFrameSize - stepSize;

            if (_rover == 0)
            {
                _rover = inFifoLatency;
            }

            /* main processing loop */
            for (var i = 0 /*offset*/; i < sampleCount; i++)
            {
                /* As long as we have not yet collected enough data just read in */
                _inFiFo[_rover] = inData[i];
                outData[i] = _outFiFo[_rover - inFifoLatency];
                _rover++;

                /* now we have enough data for processing */
                if (_rover >= fftFrameSize)
                {
                    _rover = inFifoLatency;

                    /* do windowing and re,im interleave */
                    double window;
                    long k;
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * Math.PI * k / fftFrameSize) + .5;
                        _fftWorksp[2 * k] = (float)(_inFiFo[k] * window);
                        _fftWorksp[2 * k + 1] = 0.0F;
                    }

                    /* ***************** ANALYSIS ******************* */
                    /* do transform */
                    ShortTimeFourierTransform(_fftWorksp, fftFrameSize, -1);

                    /* this is the analysis step */
                    double magn;
                    double phase;
                    double tmp;
                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        /* de-interlace FFT buffer */
                        double real = _fftWorksp[2 * k];
                        double imag = _fftWorksp[2 * k + 1];

                        /* compute magnitude and phase */
                        magn = 2.0 * Math.Sqrt(real * real + imag * imag);
                        phase = Math.Atan2(imag, real);

                        /* compute phase difference */
                        tmp = phase - _lastPhase[k];
                        _lastPhase[k] = (float)phase;

                        /* subtract expected phase difference */
                        tmp -= k * expct;

                        /* map delta phase into +/- Pi interval */
                        var qpd = (long)(tmp / Math.PI);
                        if (qpd >= 0) qpd += qpd & 1;
                        else qpd -= qpd & 1;
                        tmp -= Math.PI * qpd;

                        /* get deviation from bin frequency from the +/- Pi interval */
                        tmp = osamp * tmp / (2.0 * Math.PI);

                        /* compute the k-th partials' true frequency */
                        tmp = k * freqPerBin + tmp * freqPerBin;

                        /* store magnitude and true frequency in analysis arrays */
                        _anaMagn[k] = (float)magn;
                        _anaFreq[k] = (float)tmp;
                    }

                    /* ***************** PROCESSING ******************* */
                    /* this does the actual pitch shifting */

                    for (var zero = 0; zero < fftFrameSize; zero++)
                    {
                        _synMagn[zero] = 0;
                        _synFreq[zero] = 0;
                    }

                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        var index = (long)(k * pitchShift);
                        if (index <= fftFrameSize2)
                        {
                            _synMagn[index] += _anaMagn[k];
                            _synFreq[index] = _anaFreq[k] * pitchShift;
                        }
                    }

                    /* ***************** SYNTHESIS ******************* */
                    /* this is the synthesis step */
                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        /* get magnitude and true frequency from synthesis arrays */
                        magn = _synMagn[k];
                        tmp = _synFreq[k];

                        /* subtract bin mid frequency */
                        tmp -= k * freqPerBin;

                        /* get bin deviation from freq deviation */
                        tmp /= freqPerBin;

                        /* take osamp into account */
                        tmp = 2.0 * Math.PI * tmp / osamp;

                        /* add the overlap phase advance back in */
                        tmp += k * expct;

                        /* accumulate delta phase to get bin phase */
                        _sumPhase[k] += (float)tmp;
                        phase = _sumPhase[k];

                        /* get real and imag part and re-interleave */
                        _fftWorksp[2 * k] = (float)(magn * Math.Cos(phase));
                        _fftWorksp[2 * k + 1] = (float)(magn * Math.Sin(phase));
                    }

                    /* zero negative frequencies */
                    for (k = fftFrameSize + 2; k < 2 * fftFrameSize; k++) _fftWorksp[k] = 0.0F;

                    /* do inverse transform */
                    ShortTimeFourierTransform(_fftWorksp, fftFrameSize, 1);

                    /* do windowing and add to output accumulator */
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * Math.PI * k / fftFrameSize) + .5;
                        _outputAccum[k] += (float)(2.0 * window * _fftWorksp[2 * k] / (fftFrameSize2 * osamp));
                    }
                    for (k = 0; k < stepSize; k++) _outFiFo[k] = _outputAccum[k];

                    /* shift accumulator */
                    //memmove(gOutputAccum, gOutputAccum + stepSize, fftFrameSize * sizeof(float));
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        _outputAccum[k] = _outputAccum[k + stepSize];
                    }

                    /* move input FIFO */
                    for (k = 0; k < inFifoLatency; k++) _inFiFo[k] = _inFiFo[k + stepSize];
                }
            }
        }
    }
}