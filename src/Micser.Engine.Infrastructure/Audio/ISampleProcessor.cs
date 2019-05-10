using CSCore;

namespace Micser.Engine.Infrastructure.Audio
{
    /// <summary>
    /// A sample processor that can be registered in an <see cref="AudioModule"/> and receives every input sample that is streamed through the module.
    /// </summary>
    public interface ISampleProcessor
    {
        /// <summary>
        /// Gets or sets a value whether the sample processor is active and will receive the samples sent through the module.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the priority/order in which the processor is called amongst other processors in the same module.
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// Processes a sample value.
        /// </summary>
        /// <param name="waveFormat">The format of the samples.</param>
        /// <param name="value">The current sample value that can be modified to apply effects.</param>
        void Process(WaveFormat waveFormat, ref float value);
    }
}