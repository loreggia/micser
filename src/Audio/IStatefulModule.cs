namespace Micser.Audio;

/// <summary>
/// A module whose parameters are described by a state record. The state is what the API exposes and the
/// configuration persists; it doesn't include volume, mute and bypass, which every module has.
/// </summary>
/// <typeparam name="TState">An immutable record; each module type needs its own state type.</typeparam>
public interface IStatefulModule<TState>
    where TState : class
{
    TState GetState();

    /// <summary>
    /// Applies a state. Callers validate it (data annotations) before.
    /// </summary>
    void SetState(TState state);
}
