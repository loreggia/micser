using Microsoft.Extensions.DependencyInjection;

namespace Micser.Audio;

/// <summary>
/// Describes a module type a plugin provides. Registered with <see cref="AudioModuleServiceCollectionExtensions.AddAudioModule{TModule, TState}"/>.
/// </summary>
public sealed class AudioModuleDefinition
{
    private readonly Func<AudioModule, object> _getState;
    private readonly Action<AudioModule, object> _setState;

    private AudioModuleDefinition(
        string type,
        Type moduleType,
        Type stateType,
        Func<AudioModule, object> getState,
        Action<AudioModule, object> setState
    )
    {
        Type = type;
        ModuleType = moduleType;
        StateType = stateType;
        _getState = getState;
        _setState = setState;
    }

    public Type ModuleType { get; }

    public Type StateType { get; }

    /// <summary>
    /// The name identifying the module type in the API and the configuration, e.g. "Gain".
    /// </summary>
    public string Type { get; }

    public static AudioModuleDefinition Create<TModule, TState>(string type)
        where TModule : AudioModule, IStatefulModule<TState>
        where TState : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        return new AudioModuleDefinition(
            type,
            typeof(TModule),
            typeof(TState),
            module => ((TModule)module).GetState(),
            (module, state) => ((TModule)module).SetState((TState)state)
        );
    }

    /// <summary>
    /// Creates a module, resolving its constructor dependencies from <paramref name="services"/>.
    /// </summary>
    public AudioModule CreateModule(IServiceProvider services)
    {
        return (AudioModule)ActivatorUtilities.CreateInstance(services, ModuleType);
    }

    public object GetState(AudioModule module)
    {
        return _getState(module);
    }

    public void SetState(AudioModule module, object state)
    {
        _setState(module, state);
    }
}

public static class AudioModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAudioModule<TModule, TState>(this IServiceCollection services, string type)
        where TModule : AudioModule, IStatefulModule<TState>
        where TState : class
    {
        return services.AddSingleton(AudioModuleDefinition.Create<TModule, TState>(type));
    }
}
