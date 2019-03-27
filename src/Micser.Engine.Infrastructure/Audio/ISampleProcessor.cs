using CSCore;

namespace Micser.Engine.Infrastructure.Audio
{
    public interface ISampleProcessor
    {
        bool IsEnabled { get; set; }
        int Priority { get; set; }

        void Process(WaveFormat waveFormat, ref float value);
    }
}