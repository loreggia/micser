namespace Micser.Plugins.Main.Dsp;

/// <summary>
/// Shifts the pitch of a signal without changing its duration (short-time Fourier transform, phase vocoder). NAudio's
/// <c>SmbPitchShifter</c> with buffers for FFT sizes up to the one given to the constructor and a <see cref="Reset"/>, so a change of the
/// FFT size or the signal needs no new instance.
/// </summary>
/// <remarks>
/// Based on smbPitchShift.cpp, COPYRIGHT 1999-2006 Stephan M. Bernsee &lt;smb [AT] dspdimension [DOT] com&gt;, The Wide Open License
/// (WOL): Permission to use, copy, modify, distribute and sell this software and its documentation for any purpose is hereby granted
/// without fee, provided that the above copyright notice and this license appear in all source copies. THIS SOFTWARE IS PROVIDED "AS IS"
/// WITHOUT EXPRESS OR IMPLIED WARRANTY OF ANY KIND. See http://www.dspguru.com/wol.htm for more information.
/// Converted to C# by Michael Knight for NAudio.
/// </remarks>
internal sealed class PitchShifter
{
    private readonly float[] _anaFreq;
    private readonly float[] _anaMagn;
    private readonly float[] _fftWorkspace;
    private readonly float[] _inFifo;
    private readonly float[] _lastPhase;
    private readonly int _maxFftSize;
    private readonly float[] _outFifo;
    private readonly float[] _outputAccum;
    private readonly float[] _sumPhase;
    private readonly float[] _synFreq;
    private readonly float[] _synMagn;
    private int _fftSize;
    private int _rover;

    /// <param name="maxFftSize">The largest FFT size <see cref="Process"/> is called with.</param>
    public PitchShifter(int maxFftSize)
    {
        _maxFftSize = maxFftSize;
        _inFifo = new float[maxFftSize];
        _outFifo = new float[maxFftSize];
        _fftWorkspace = new float[2 * maxFftSize];
        _lastPhase = new float[(maxFftSize / 2) + 1];
        _sumPhase = new float[(maxFftSize / 2) + 1];
        _outputAccum = new float[2 * maxFftSize];
        _anaFreq = new float[(maxFftSize / 2) + 1];
        _anaMagn = new float[(maxFftSize / 2) + 1];
        _synFreq = new float[maxFftSize];
        _synMagn = new float[maxFftSize];
    }

    /// <summary>
    /// Clears the state; the next block starts with the latency of an FFT frame of silence, as a new instance does.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_inFifo);
        Array.Clear(_outFifo);
        Array.Clear(_fftWorkspace);
        Array.Clear(_lastPhase);
        Array.Clear(_sumPhase);
        Array.Clear(_outputAccum);
        Array.Clear(_anaFreq);
        Array.Clear(_anaMagn);
        Array.Clear(_synFreq);
        Array.Clear(_synMagn);
        _rover = 0;
    }

    /// <summary>
    /// Shifts the pitch of <paramref name="samples"/> in place. A change of <paramref name="fftSize"/> resets the state.
    /// </summary>
    /// <param name="pitchShift">The frequency factor, 0.5 (one octave down) to 2 (one octave up).</param>
    /// <param name="fftSize">A power of two up to the maximum given to the constructor.</param>
    /// <param name="oversampling">The overlap of the frames; at least 4 for moderate factors.</param>
    public void Process(float pitchShift, int fftSize, int oversampling, float sampleRate, Span<float> samples)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fftSize, _maxFftSize);
        if (fftSize != _fftSize)
        {
            Reset();
            _fftSize = fftSize;
        }

        var halfSize = fftSize / 2;
        var stepSize = fftSize / oversampling;
        var freqPerBin = sampleRate / (double)fftSize;
        var expected = 2.0 * Math.PI * stepSize / fftSize;
        var inFifoLatency = fftSize - stepSize;
        if (_rover == 0)
        {
            _rover = inFifoLatency;
        }

        for (var i = 0; i < samples.Length; i++)
        {
            // as long as there isn't enough data, just read in
            _inFifo[_rover] = samples[i];
            samples[i] = _outFifo[_rover - inFifoLatency];
            _rover++;

            if (_rover >= fftSize)
            {
                _rover = inFifoLatency;
                ProcessFrame(
                    pitchShift,
                    fftSize,
                    oversampling,
                    halfSize,
                    stepSize,
                    freqPerBin,
                    expected,
                    inFifoLatency
                );
            }
        }
    }

    private static void Transform(Span<float> buffer, int fftSize, int sign)
    {
        for (var i = 2; i < (2 * fftSize) - 2; i += 2)
        {
            var j = 0;
            for (var bitm = 2; bitm < 2 * fftSize; bitm <<= 1)
            {
                if ((i & bitm) != 0)
                {
                    j++;
                }

                j <<= 1;
            }

            if (i < j)
            {
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
                (buffer[i + 1], buffer[j + 1]) = (buffer[j + 1], buffer[i + 1]);
            }
        }

        var stages = (int)((Math.Log(fftSize) / Math.Log(2.0)) + .5);
        for (int k = 0, le = 2; k < stages; k++)
        {
            le <<= 1;
            var le2 = le >> 1;
            var ur = 1.0f;
            var ui = 0.0f;
            var arg = (float)Math.PI / (le2 >> 1);
            var wr = (float)Math.Cos(arg);
            var wi = (float)(sign * Math.Sin(arg));
            for (var j = 0; j < le2; j += 2)
            {
                for (var i = j; i < 2 * fftSize; i += le)
                {
                    var tr = (buffer[i + le2] * ur) - (buffer[i + le2 + 1] * ui);
                    var ti = (buffer[i + le2] * ui) + (buffer[i + le2 + 1] * ur);
                    buffer[i + le2] = buffer[i] - tr;
                    buffer[i + le2 + 1] = buffer[i + 1] - ti;
                    buffer[i] += tr;
                    buffer[i + 1] += ti;
                }

                var next = (ur * wr) - (ui * wi);
                ui = (ur * wi) + (ui * wr);
                ur = next;
            }
        }
    }

    private void ProcessFrame(
        float pitchShift,
        int fftSize,
        int oversampling,
        int halfSize,
        int stepSize,
        double freqPerBin,
        double expected,
        int inFifoLatency
    )
    {
        // windowing and re, im interleave
        for (var k = 0; k < fftSize; k++)
        {
            var window = (-.5 * Math.Cos(2.0 * Math.PI * k / fftSize)) + .5;
            _fftWorkspace[2 * k] = (float)(_inFifo[k] * window);
            _fftWorkspace[(2 * k) + 1] = 0.0f;
        }

        // analysis
        Transform(_fftWorkspace, fftSize, -1);
        for (var k = 0; k <= halfSize; k++)
        {
            double real = _fftWorkspace[2 * k];
            double imag = _fftWorkspace[(2 * k) + 1];
            var magn = 2.0 * Math.Sqrt((real * real) + (imag * imag));
            var phase = Math.Atan2(imag, real);

            // the phase difference, without the expected one, mapped into +/- pi
            var tmp = phase - _lastPhase[k];
            _lastPhase[k] = (float)phase;
            tmp -= k * expected;
            var qpd = (int)(tmp / Math.PI);
            qpd = qpd >= 0 ? qpd + (qpd & 1) : qpd - (qpd & 1);
            tmp -= Math.PI * qpd;

            // the partial's true frequency from its deviation from the bin frequency
            tmp = oversampling * tmp / (2.0 * Math.PI);
            tmp = (k * freqPerBin) + (tmp * freqPerBin);

            _anaMagn[k] = (float)magn;
            _anaFreq[k] = (float)tmp;
        }

        // pitch shifting
        _synMagn.AsSpan(0, fftSize).Clear();
        _synFreq.AsSpan(0, fftSize).Clear();
        for (var k = 0; k <= halfSize; k++)
        {
            var index = (int)(k * pitchShift);
            if (index <= halfSize)
            {
                _synMagn[index] += _anaMagn[k];
                _synFreq[index] = _anaFreq[k] * pitchShift;
            }
        }

        // synthesis
        for (var k = 0; k <= halfSize; k++)
        {
            double magn = _synMagn[k];
            double tmp = _synFreq[k];
            tmp -= k * freqPerBin;
            tmp /= freqPerBin;
            tmp = 2.0 * Math.PI * tmp / oversampling;
            tmp += k * expected;

            _sumPhase[k] += (float)tmp;
            double phase = _sumPhase[k];
            _fftWorkspace[2 * k] = (float)(magn * Math.Cos(phase));
            _fftWorkspace[(2 * k) + 1] = (float)(magn * Math.Sin(phase));
        }

        _fftWorkspace.AsSpan(fftSize + 2, fftSize - 2).Clear();
        Transform(_fftWorkspace, fftSize, 1);

        // windowing and overlap-add
        for (var k = 0; k < fftSize; k++)
        {
            var window = (-.5 * Math.Cos(2.0 * Math.PI * k / fftSize)) + .5;
            _outputAccum[k] += (float)(2.0 * window * _fftWorkspace[2 * k] / (halfSize * oversampling));
        }

        _outputAccum.AsSpan(0, stepSize).CopyTo(_outFifo);
        for (var k = 0; k < fftSize; k++)
        {
            _outputAccum[k] = _outputAccum[k + stepSize];
        }

        for (var k = 0; k < inFifoLatency; k++)
        {
            _inFifo[k] = _inFifo[k + stepSize];
        }
    }
}
