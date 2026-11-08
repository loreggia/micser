namespace Micser.Plugins.Main.Dsp;

/// <summary>
/// Shifts the pitch and the formants of a voice independently (short-time Fourier transform, phase vocoder). Each frame splits the spectrum
/// into its envelope (the formants, a cepstral "true envelope") and the excitation (the harmonics), moves the harmonics by the pitch factor and
/// the envelope by the formant factor. Breathiness, wobble and the <see cref="VoiceTexture"/> change the synthesis. The dry signal is delayed by the latency, so mixing it in doesn't comb-filter. Buffers are sized for
/// the FFT size given to the constructor, so a change of the FFT size or the signal needs no new instance.
/// </summary>
/// <remarks>The phase vocoder follows <see cref="PitchShifter"/> (smbPitchShift.cpp by Stephan M. Bernsee).</remarks>
internal sealed class VoiceShifter
{
    /// <summary>
    /// The quefrency up to which the cepstrum counts as envelope, tapered towards it: below the pitch period of high voices (2.5 ms at
    /// 400 Hz), above the detail of the formants.
    /// </summary>
    private const double CepstrumCutoffSeconds = 0.0024;

    /// <summary>
    /// The smoothing passes of the true envelope; each lifts the envelope towards the harmonic peaks it passes below.
    /// </summary>
    private const int EnvelopeIterations = 4;

    /// <summary>
    /// The level of breath and whisper noise relative to the envelope.
    /// </summary>
    private const float NoiseLevel = 0.5f;

    /// <summary>
    /// Breath noise fades in up to this frequency, so it sounds like air rather than rumble.
    /// </summary>
    private const float NoiseFadeInHertz = 1000f;

    /// <summary>
    /// The pitch deviation of full wobble, in semitones.
    /// </summary>
    private const float WobbleSemitones = 0.8f;

    /// <summary>
    /// The rate of the wobble in Hz, about that of a shaky voice.
    /// </summary>
    private const double WobbleRate = 5.5;

    /// <summary>
    /// How far full wobble lowers the level at the bottom of each cycle.
    /// </summary>
    private const float WobbleTremolo = 0.3f;

    /// <summary>
    /// The largest boost of a bin by the envelope correction, so a trough between formants that moves under a peak doesn't blow up noise.
    /// </summary>
    private const float MaxGain = 16f;

    /// <summary>
    /// How far the pitch and formant factors move towards their targets per frame, so parameter changes don't click.
    /// </summary>
    private const float Smoothing = 0.25f;

    private readonly float[] _anaFreq;
    private readonly float[] _anaMagn;
    private readonly float[] _cepstrum;
    private readonly Fft _fft;
    private readonly float[] _inFifo;
    private readonly float[] _lastPhase;
    private readonly float[] _logEnvelope;
    private readonly float[] _logMagn;
    private readonly int _maxFftSize;
    private readonly float[] _outFifo;
    private readonly float[] _outputAccum;
    private readonly float[] _spectrum;
    private readonly float[] _sumPhase;
    private readonly float[] _synFreq;
    private readonly float[] _synMagn;
    private readonly float[] _window;
    private int _fftSize;
    private float _breathiness;
    private float _formantFactor = float.NaN;
    private double _lfoPhase;
    private float _mix = float.NaN;
    private int _oversampling;
    private float _wobble;
    private float _pitchFactor = float.NaN;
    private uint _random = RandomSeed;
    private double _robotPhase;
    private int _rover;

    /// <summary>
    /// The note of the <see cref="VoiceTexture.Robot"/> texture before the pitch factor, in Hz.
    /// </summary>
    public const float RobotFrequency = 110f;

    private const uint RandomSeed = 0x9E3779B9;

    /// <param name="maxFftSize">The largest FFT size <see cref="Process"/> is called with.</param>
    public VoiceShifter(int maxFftSize)
    {
        _maxFftSize = maxFftSize;
        _fft = new Fft(maxFftSize);
        _inFifo = new float[maxFftSize];
        _outFifo = new float[maxFftSize];
        _outputAccum = new float[2 * maxFftSize];
        _spectrum = new float[2 * maxFftSize];
        _cepstrum = new float[2 * maxFftSize];
        _window = new float[maxFftSize];
        _lastPhase = new float[(maxFftSize / 2) + 1];
        _sumPhase = new float[(maxFftSize / 2) + 1];
        _anaFreq = new float[(maxFftSize / 2) + 1];
        _anaMagn = new float[(maxFftSize / 2) + 1];
        _logEnvelope = new float[(maxFftSize / 2) + 1];
        _logMagn = new float[(maxFftSize / 2) + 1];
        _synFreq = new float[(maxFftSize / 2) + 1];
        _synMagn = new float[(maxFftSize / 2) + 1];
    }

    /// <summary>
    /// The delay of the output in samples for an FFT size and oversampling.
    /// </summary>
    public static int GetLatency(int fftSize, int oversampling)
    {
        return fftSize - (fftSize / oversampling);
    }

    /// <summary>
    /// Clears the state; the next block starts with the latency of an FFT frame of silence, as a new instance does.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_inFifo);
        Array.Clear(_outFifo);
        Array.Clear(_outputAccum);
        Array.Clear(_lastPhase);
        Array.Clear(_sumPhase);
        _rover = 0;
        _pitchFactor = float.NaN;
        _formantFactor = float.NaN;
        _mix = float.NaN;
        _breathiness = 0f;
        _wobble = 0f;
        _lfoPhase = 0;
        _robotPhase = 0;
        _random = RandomSeed;
    }

    /// <summary>
    /// Processes <paramref name="samples"/> in place. A change of <paramref name="fftSize"/> or <paramref name="oversampling"/> resets the
    /// state.
    /// </summary>
    /// <param name="fftSize">A power of two up to the maximum given to the constructor.</param>
    /// <param name="oversampling">The overlap of the frames, a power of two of at least 4 that divides the FFT size.</param>
    public void Process(
        in VoiceParameters parameters,
        int fftSize,
        int oversampling,
        float sampleRate,
        Span<float> samples
    )
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fftSize, _maxFftSize);
        // a new hop size moves the latency and with it the FIFO positions
        if (fftSize != _fftSize || oversampling != _oversampling)
        {
            Reset();
            _oversampling = oversampling;
            if (fftSize != _fftSize)
            {
                _fftSize = fftSize;
                HannWindow.Fill(_window.AsSpan(0, fftSize));
            }
        }

        var (pitchFactor, formantFactor, mix, breathiness, wobble, texture) = parameters;
        if (float.IsNaN(_pitchFactor))
        {
            _pitchFactor = pitchFactor;
            _formantFactor = formantFactor;
            _mix = mix;
            _breathiness = breathiness;
            _wobble = wobble;
        }

        var stepSize = fftSize / oversampling;
        var latency = fftSize - stepSize;
        if (_rover == 0)
        {
            _rover = latency;
        }

        var currentMix = _mix;
        var mixStep = (mix - currentMix) / samples.Length;
        for (var i = 0; i < samples.Length; i++)
        {
            // the input from one latency ago, still in the input FIFO
            var dry = _inFifo[_rover - latency];
            _inFifo[_rover] = samples[i];
            var wet = _outFifo[_rover - latency];
            currentMix += mixStep;
            samples[i] = dry + (currentMix * (wet - dry));
            _rover++;

            if (_rover >= fftSize)
            {
                _rover = latency;
                _pitchFactor += (pitchFactor - _pitchFactor) * Smoothing;
                _formantFactor += (formantFactor - _formantFactor) * Smoothing;
                _breathiness += (breathiness - _breathiness) * Smoothing;
                _wobble += (wobble - _wobble) * Smoothing;
                ProcessFrame(fftSize, stepSize, sampleRate, texture);
            }
        }

        _mix = mix;
    }

    /// <summary>
    /// The smoothed log envelope at a fractional bin, the last bin's value beyond it.
    /// </summary>
    private float EnvelopeAt(float bin, int halfSize)
    {
        if (bin >= halfSize)
        {
            return _logEnvelope[halfSize];
        }

        var index = (int)bin;
        var fraction = bin - index;
        return _logEnvelope[index] + (fraction * (_logEnvelope[index + 1] - _logEnvelope[index]));
    }

    /// <summary>
    /// Fills <see cref="_logEnvelope"/> from <see cref="_anaMagn"/> with the true envelope (Röbel and Rodet): cepstral smoothing of the log
    /// spectrum, repeated on the maximum of the log spectrum and the last envelope. Plain smoothing takes the valleys between the
    /// harmonics into the average and flattens the formants; the iterations make it pass through the harmonic peaks instead.
    /// </summary>
    private void ComputeEnvelope(int fftSize, int halfSize, float sampleRate)
    {
        for (var k = 0; k <= halfSize; k++)
        {
            _logMagn[k] = MathF.Log(_anaMagn[k] + 1e-9f);
        }

        var cutoff = Math.Clamp((int)(sampleRate * CepstrumCutoffSeconds), 1, halfSize - 1);
        Smooth(_logMagn, fftSize, halfSize, cutoff);
        for (var i = 1; i < EnvelopeIterations; i++)
        {
            for (var k = 0; k <= halfSize; k++)
            {
                _logEnvelope[k] = MathF.Max(_logEnvelope[k], _logMagn[k]);
            }

            Smooth(_logEnvelope, fftSize, halfSize, cutoff);
        }
    }

    /// <summary>
    /// Writes <paramref name="logSpectrum"/> without its quefrencies from <paramref name="cutoff"/> on (tapered towards it) to <see cref="_logEnvelope"/>, which
    /// may be the same array.
    /// </summary>
    private void Smooth(float[] logSpectrum, int fftSize, int halfSize, int cutoff)
    {
        var cepstrum = _cepstrum.AsSpan(0, 2 * fftSize);
        for (var k = 0; k <= halfSize; k++)
        {
            cepstrum[2 * k] = logSpectrum[k];
            cepstrum[(2 * k) + 1] = 0f;
            if (k > 0 && k < halfSize)
            {
                cepstrum[2 * (fftSize - k)] = logSpectrum[k];
                cepstrum[(2 * (fftSize - k)) + 1] = 0f;
            }
        }

        _fft.Transform(cepstrum, fftSize, inverse: true);

        var scale = 1f / fftSize;
        for (var n = 0; n < fftSize; n++)
        {
            var quefrency = n <= halfSize ? n : fftSize - n;
            // a Hann taper: a hard cutoff rings as a regular ripple across the envelope (every 1 / cutoff in frequency), which makes
            // noise shaped by it sound tonal
            var weight = quefrency < cutoff ? 0.5f + (0.5f * MathF.Cos(MathF.PI * quefrency / cutoff)) : 0f;
            cepstrum[2 * n] *= scale * weight;
            cepstrum[(2 * n) + 1] = 0f;
        }

        _fft.Transform(cepstrum, fftSize, inverse: false);
        for (var k = 0; k <= halfSize; k++)
        {
            _logEnvelope[k] = cepstrum[2 * k];
        }
    }

    /// <summary>
    /// A uniform random phase in [0, 2 pi) (xorshift32, deterministic from the seed).
    /// </summary>
    private double NextPhase()
    {
        var x = _random;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _random = x;
        return 2.0 * Math.PI * (x >> 8) / 16777216.0;
    }

    private void ProcessFrame(int fftSize, int stepSize, float sampleRate, VoiceTexture texture)
    {
        var halfSize = fftSize / 2;
        var oversampling = fftSize / stepSize;
        var freqPerBin = sampleRate / (double)fftSize;
        var expected = 2.0 * Math.PI * stepSize / fftSize;
        var spectrum = _spectrum.AsSpan(0, 2 * fftSize);

        for (var k = 0; k < fftSize; k++)
        {
            spectrum[2 * k] = _inFifo[k] * _window[k];
            spectrum[(2 * k) + 1] = 0f;
        }

        // analysis: magnitude and true frequency per bin
        _fft.Transform(spectrum, fftSize, inverse: false);
        for (var k = 0; k <= halfSize; k++)
        {
            double real = spectrum[2 * k];
            double imag = spectrum[(2 * k) + 1];
            var phase = Math.Atan2(imag, real);

            // the phase difference, without the expected one, mapped into +/- pi
            var delta = phase - _lastPhase[k];
            _lastPhase[k] = (float)phase;
            delta -= k * expected;
            var qpd = (int)(delta / Math.PI);
            qpd = qpd >= 0 ? qpd + (qpd & 1) : qpd - (qpd & 1);
            delta -= Math.PI * qpd;

            _anaMagn[k] = (float)(2.0 * Math.Sqrt((real * real) + (imag * imag)));
            _anaFreq[k] = (float)((k + (oversampling * delta / (2.0 * Math.PI))) * freqPerBin);
        }

        ComputeEnvelope(fftSize, halfSize, sampleRate);

        // the wobble modulates pitch and level at frame rate
        var lfo = Math.Sin(_lfoPhase);
        _lfoPhase = Math.IEEERemainder(_lfoPhase + (2.0 * Math.PI * WobbleRate * stepSize / sampleRate), 2.0 * Math.PI);
        var pitch = _pitchFactor * MathF.Pow(2f, _wobble * WobbleSemitones * (float)lfo / 12f);
        var level = 1f - (_wobble * WobbleTremolo * (0.5f + (0.5f * (float)lfo)));
        var formant = _formantFactor;

        // the harmonics move by the pitch factor, the envelope by the formant factor
        _synMagn.AsSpan(0, halfSize + 1).Clear();
        _synFreq.AsSpan(0, halfSize + 1).Clear();
        if (texture != VoiceTexture.Whisper)
        {
            for (var k = 0; k <= halfSize; k++)
            {
                var index = (int)(k * pitch);
                if (index > halfSize)
                {
                    break;
                }

                var gain = MathF.Min(MaxGain, MathF.Exp(EnvelopeAt(index / formant, halfSize) - _logEnvelope[k]));
                _synMagn[index] += _anaMagn[k] * gain;
                _synFreq[index] = _anaFreq[k] * pitch;
            }
        }

        // the robot's harmonics all follow one phase, so the bins of a harmonic add up coherently
        var robotFrequency = RobotFrequency * pitch;
        var robotPhase = _robotPhase;
        _robotPhase = Math.IEEERemainder(
            _robotPhase + (2.0 * Math.PI * robotFrequency * stepSize / sampleRate),
            2.0 * Math.PI
        );

        var noise = texture == VoiceTexture.Whisper ? NoiseLevel : _breathiness * NoiseLevel;
        var harmonics = texture == VoiceTexture.Whisper ? 0f : 1f - (0.5f * _breathiness);

        // synthesis
        for (var k = 0; k <= halfSize; k++)
        {
            double phase;
            if (texture == VoiceTexture.Robot)
            {
                var harmonic = Math.Max(1.0, Math.Round(_synFreq[k] / robotFrequency));
                phase = Math.IEEERemainder(harmonic * robotPhase, 2.0 * Math.PI);
            }
            else
            {
                var deviation = (_synFreq[k] / freqPerBin) - k;
                // wrapped, so the float keeps its precision over long runs
                phase = Math.IEEERemainder(
                    _sumPhase[k] + (2.0 * Math.PI * deviation / oversampling) + (k * expected),
                    2.0 * Math.PI
                );
                _sumPhase[k] = (float)phase;
            }

            var magnitude = level * harmonics * _synMagn[k];
            var real = magnitude * Math.Cos(phase);
            var imag = magnitude * Math.Sin(phase);
            if (noise > 0f)
            {
                var fadeIn =
                    texture == VoiceTexture.Whisper ? 1f : MathF.Min(1f, (float)(k * freqPerBin) / NoiseFadeInHertz);
                var noiseMagnitude = level * noise * fadeIn * MathF.Exp(EnvelopeAt(k / formant, halfSize));
                var noisePhase = NextPhase();
                real += noiseMagnitude * Math.Cos(noisePhase);
                imag += noiseMagnitude * Math.Sin(noisePhase);
            }

            spectrum[2 * k] = (float)real;
            spectrum[(2 * k) + 1] = (float)imag;
        }

        spectrum[(2 * (halfSize + 1))..].Clear();
        _fft.Transform(spectrum, fftSize, inverse: true);

        // windowing and overlap-add
        var scale = 2f / (halfSize * oversampling);
        for (var k = 0; k < fftSize; k++)
        {
            _outputAccum[k] += scale * _window[k] * spectrum[2 * k];
        }

        _outputAccum.AsSpan(0, stepSize).CopyTo(_outFifo);
        _outputAccum.AsSpan(stepSize, fftSize).CopyTo(_outputAccum);
        _inFifo.AsSpan(stepSize, fftSize - stepSize).CopyTo(_inFifo);
    }
}
