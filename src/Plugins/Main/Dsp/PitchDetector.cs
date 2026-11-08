namespace Micser.Plugins.Main.Dsp;

/// <summary>
/// Detects the pitch of a voice with YIN (de Cheveigné and Kawahara): the cumulative mean normalized difference function of the latest
/// samples, with the autocorrelation computed by FFT so it runs once per STFT frame on the audio thread. Buffers are sized for the
/// highest sample rate given to the constructor.
/// </summary>
internal sealed class PitchDetector
{
    /// <summary>
    /// The highest detected pitch in Hz, above a child's or a soprano's speaking voice.
    /// </summary>
    public const float MaxFrequency = 1000f;

    /// <summary>
    /// The lowest detected pitch in Hz, below a deep male voice.
    /// </summary>
    public const float MinFrequency = 60f;

    /// <summary>
    /// The difference below which a lag counts as a period; YIN's paper suggests 0.1 to 0.15.
    /// </summary>
    private const float Threshold = 0.15f;

    /// <summary>
    /// The mean square below which the input counts as silence (-60 dBFS).
    /// </summary>
    private const float SilenceLevel = 1e-6f;

    private readonly float[] _difference;
    private readonly float[] _energy;
    private readonly Fft _fft;
    private readonly float[] _frame;
    private readonly float[] _history;
    private readonly float[] _lagSpectrum;
    private int _position;

    /// <param name="maxSampleRate">The highest sample rate <see cref="Detect"/> is called with.</param>
    public PitchDetector(int maxSampleRate)
    {
        var maxSize = GetSize(maxSampleRate);
        _fft = new Fft(maxSize);
        _history = new float[maxSize];
        _frame = new float[2 * maxSize];
        _lagSpectrum = new float[2 * maxSize];
        _energy = new float[maxSize + 1];
        _difference = new float[(maxSize / 2) + 1];
    }

    /// <summary>
    /// The number of samples the detection looks at: the smallest power of two whose half holds a period of <see cref="MinFrequency"/>,
    /// since the lags go up to half of it (2048 at 48 kHz, 43 ms).
    /// </summary>
    public static int GetSize(int sampleRate)
    {
        var maxLag = (int)MathF.Ceiling(sampleRate / MinFrequency);
        return (int)Math.Max(256, System.Numerics.BitOperations.RoundUpToPowerOf2((uint)(2 * maxLag)));
    }

    public void Reset()
    {
        Array.Clear(_history);
        _position = 0;
    }

    /// <summary>
    /// Adds a sample to the history the detection looks at.
    /// </summary>
    public void Write(float sample)
    {
        _history[_position] = sample;
        _position = (_position + 1) % _history.Length;
    }

    /// <summary>
    /// The pitch of the latest samples in Hz, or 0 when they're silent or have no clear period (noise, breath, consonants), or the sample
    /// rate is above the maximum.
    /// </summary>
    public float Detect(int sampleRate)
    {
        var size = GetSize(sampleRate);
        if (size > _history.Length)
        {
            return 0f;
        }

        var window = size / 2;

        // the latest samples in order, and their running energy
        var frame = _frame.AsSpan(0, 2 * size);
        var start = _position - size + _history.Length;
        _energy[0] = 0f;
        for (var j = 0; j < size; j++)
        {
            var sample = _history[(start + j) % _history.Length];
            frame[2 * j] = sample;
            frame[(2 * j) + 1] = 0f;
            _energy[j + 1] = _energy[j] + (sample * sample);
        }

        if (_energy[window] / window < SilenceLevel)
        {
            return 0f;
        }

        // r(lag) = sum over the first half of x[j] x[j + lag], as the correlation of the frame with its zero-padded first half
        var lagSpectrum = _lagSpectrum.AsSpan(0, 2 * size);
        frame[..(2 * window)].CopyTo(lagSpectrum);
        lagSpectrum[(2 * window)..].Clear();
        _fft.Transform(frame, size, inverse: false);
        _fft.Transform(lagSpectrum, size, inverse: false);
        for (var k = 0; k < size; k++)
        {
            var (ar, ai) = (frame[2 * k], frame[(2 * k) + 1]);
            var (br, bi) = (lagSpectrum[2 * k], lagSpectrum[(2 * k) + 1]);
            frame[2 * k] = (ar * br) + (ai * bi);
            frame[(2 * k) + 1] = (ai * br) - (ar * bi);
        }

        _fft.Transform(frame, size, inverse: true);

        // the cumulative mean normalized difference
        var minLag = Math.Max(2, (int)(sampleRate / MaxFrequency));
        var maxLag = Math.Min(window, (int)MathF.Ceiling(sampleRate / MinFrequency));
        var sum = 0f;
        _difference[0] = 1f;
        for (var lag = 1; lag <= maxLag; lag++)
        {
            var difference = _energy[window] + (_energy[lag + window] - _energy[lag]) - (2f * frame[2 * lag] / size);
            sum += difference;
            _difference[lag] = sum > 0f ? difference * lag / sum : 1f;
        }

        // the first dip below the threshold, followed to its minimum
        for (var lag = minLag; lag < maxLag; lag++)
        {
            if (_difference[lag] >= Threshold)
            {
                continue;
            }

            while (lag + 1 < maxLag && _difference[lag + 1] < _difference[lag])
            {
                lag++;
            }

            return sampleRate / Interpolate(lag);
        }

        return 0f;
    }

    /// <summary>
    /// The lag of the minimum around <paramref name="lag"/>, from a parabola through it and its neighbours.
    /// </summary>
    private float Interpolate(int lag)
    {
        var (left, center, right) = (_difference[lag - 1], _difference[lag], _difference[lag + 1]);
        var denominator = left - (2f * center) + right;
        return denominator > 0f ? lag + (0.5f * (left - right) / denominator) : lag;
    }
}
