using Microsoft.Extensions.DependencyInjection;

namespace Micser.Audio;

/// <summary>
/// The entry point of a plugin assembly. The engine creates the plugin's only public implementation with its parameterless constructor.
/// </summary>
public interface IAudioPlugin
{
    /// <summary>
    /// Registers the plugin's module types (<see cref="AudioModuleServiceCollectionExtensions.AddAudioModule{TModule, TState}"/>) and services.
    /// </summary>
    void ConfigureServices(IServiceCollection services);
}
