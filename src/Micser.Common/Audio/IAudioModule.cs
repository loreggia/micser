using System;
using CSCore;
using Micser.Common.Modules;

namespace Micser.Common.Audio
{
    /// <summary>
    /// Describes an audio module that can be used by the audio engine.
    /// </summary>
    public interface IAudioModule : IIdentifiable, IDisposable
    {
        /// <summary>
        /// Gets or sets whether the module is enabled. A disabled module will pass through the audio without affecting it.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets whether the module is muted and will not output any data.
        /// </summary>
        bool IsMuted { get; set; }

        /// <summary>
        /// Gets or sets whether the module uses the current system output volume and muted properties.
        /// </summary>
        bool UseSystemVolume { get; set; }

        /// <summary>
        /// Gets or sets the output volume (0..1).
        /// </summary>
        float Volume { get; set; }

        /// <summary>
        /// Adds an output connection to another module.
        /// </summary>
        /// <param name="module">The module to send audio data to.</param>
        void AddOutput(IAudioModule module);

        /// <summary>
        /// Adds a sample processor that will process each sample written to this module.
        /// </summary>
        /// <param name="sampleProcessor"></param>
        void AddSampleProcessor(ISampleProcessor sampleProcessor);

        /// <summary>
        /// Gets the module's current state.
        /// </summary>
        ModuleState GetState();

        /// <summary>
        /// Removes a module from the list of outputs.
        /// </summary>
        /// <param name="module">The module that is currently registered as an output of this module.</param>
        void RemoveOutput(IAudioModule module);

        /// <summary>
        /// Removes all sample processors that inherit type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of sample processor to remove.</typeparam>
        void RemoveSampleProcessor<T>() where T : ISampleProcessor;

        /// <summary>
        /// Removes a specific sample processor instance.
        /// </summary>
        void RemoveSampleProcessor(ISampleProcessor sampleProcessor);

        /// <summary>
        /// Sets the module's state.
        /// </summary>
        /// <param name="state">The new state to set.</param>
        void SetState(ModuleState state);

        /// <summary>
        /// Writes audio data to this module. Each sample in the <paramref name="buffer"/> will be sent through all registered <see cref="ISampleProcessor"/>s before being sent to each registered output module.
        /// </summary>
        /// <param name="source">The source audio module. This should be the original module where the data was generated from and not the previous module in the data flow.</param>
        /// <param name="waveFormat">The format of the audio data.</param>
        /// <param name="buffer">The buffer containing the audio samples.</param>
        /// <param name="offset">The offset to start reading from.</param>
        /// <param name="count">The number of bytes to read from the <paramref name="buffer"/>.</param>
        void Write(IAudioModule source, WaveFormat waveFormat, byte[] buffer, int offset, int count);
    }
}