using NAudio.Wave.SampleProviders;

namespace Micser.Main.Audio
{
    public enum WaveType
    {
        Pink,
        White,
        Sweep,
        Sin,
        Square,
        Triangle,
        SawTooth,
    }

    public class WaveGenerator : AudioChainLink
    {
        private readonly SignalGenerator _signalGenerator;

        public WaveGenerator()
        {
            _signalGenerator = new SignalGenerator();
            Frequency = 440;
            Type = WaveType.Sin;
        }

        public double Frequency
        {
            get => _signalGenerator.Frequency;
            set => _signalGenerator.Frequency = value;
        }

        public WaveType Type
        {
            get => (WaveType)(int)_signalGenerator.Type;
            set => _signalGenerator.Type = (SignalGeneratorType)value;
        }

        protected override int ReadInternal(float[] buffer, int offset, int count)
        {
            return _signalGenerator.Read(buffer, offset, count);
        }
    }
}