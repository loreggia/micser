using Micser.Audio;

namespace Micser.Plugins.Main.Tests;

/// <summary>
/// Runs a sine through a chain of effects and records the result.
/// </summary>
internal sealed class SignalTestBench
{
    public static readonly ProcessingFormat Format = new(48000, 480);

    private readonly AudioGraph _graph = new(Format);
    private readonly Recorder _recorder = new();
    private readonly SineSource _source;

    public SignalTestBench(
        float frequency,
        float amplitude,
        ChannelLayout? layout = null,
        params EffectModule[] effects
    )
    {
        _source = new SineSource(frequency, amplitude, layout ?? ChannelLayout.Mono);
        _graph.Add(_source);
        _graph.Add(_recorder);

        var previous = _source.Output;
        foreach (var effect in effects)
        {
            _graph.Add(effect);
            _graph.Connect(previous, effect.Input);
            previous = effect.Output;
        }

        _graph.Connect(previous, _recorder.Input);
    }

    public float Amplitude
    {
        get => _source.Amplitude;
        set => _source.Amplitude = value;
    }

    public float Frequency
    {
        get => _source.Frequency;
        set => _source.Frequency = value;
    }

    public ChannelLayout OutputLayout => _recorder.Layout;

    public static float Peak(ReadOnlySpan<float> samples)
    {
        var peak = 0f;
        foreach (var sample in samples)
        {
            peak = Math.Max(peak, Math.Abs(sample));
        }

        return peak;
    }

    /// <summary>
    /// Processes blocks for the given duration and returns channel 0 of the recorded output.
    /// </summary>
    public float[] Run(TimeSpan duration)
    {
        _recorder.Samples.Clear();
        var blocks = (int)Math.Ceiling(duration.TotalSeconds * Format.SampleRate / Format.FrameCount);
        for (var i = 0; i < blocks; i++)
        {
            _graph.Process();
        }

        return [.. _recorder.Samples];
    }

    private sealed class Recorder : AudioModule
    {
        public Recorder()
        {
            Input = AddInput("Input");
        }

        public InputPort Input { get; }

        public ChannelLayout Layout { get; private set; }

        public List<float> Samples { get; } = [];

        protected override void Process()
        {
            Layout = Input.Buffer.Layout;
            if (Layout.ChannelCount > 0)
            {
                Samples.AddRange(Input.Buffer.GetChannel(0));
            }
        }
    }

    private sealed class SineSource : AudioModule
    {
        private readonly ChannelLayout _layout;
        private double _phase;

        public SineSource(float frequency, float amplitude, ChannelLayout layout)
        {
            Frequency = frequency;
            Amplitude = amplitude;
            _layout = layout;
            Output = AddOutput("Output");
        }

        public float Amplitude { get; set; }

        public float Frequency { get; set; }

        public OutputPort Output { get; }

        protected override void Process()
        {
            var buffer = Output.Buffer;
            buffer.SetLayout(_layout);
            var increment = 2 * Math.PI * Frequency / Format.SampleRate;

            var first = buffer.GetChannel(0);
            for (var i = 0; i < first.Length; i++)
            {
                first[i] = Amplitude * (float)Math.Sin(_phase);
                _phase += increment;
            }

            for (var c = 1; c < buffer.ChannelCount; c++)
            {
                first.CopyTo(buffer.GetChannel(c));
            }
        }
    }
}
