namespace Micser.Plugins.Main.Modules;

/// <summary>
/// Spaces out retries: the first is due right away, then after 1 s, doubling up to 30 s.
/// </summary>
internal sealed class RetryBackoff
{
    private const long InitialDelayMilliseconds = 1000;
    private const long MaxDelayMilliseconds = 30000;

    private readonly Func<long> _clock;
    private long _delay;
    private long _due;

    /// <param name="clock">Milliseconds; default <see cref="Environment.TickCount64"/>.</param>
    public RetryBackoff(Func<long>? clock = null)
    {
        _clock = clock ?? (() => Environment.TickCount64);
    }

    public bool IsDue => _clock() >= _due;

    /// <summary>
    /// Records an attempt; the next one is due after the current delay, which then doubles.
    /// </summary>
    public void Attempted()
    {
        _delay = _delay == 0 ? InitialDelayMilliseconds : Math.Min(_delay * 2, MaxDelayMilliseconds);
        _due = _clock() + _delay;
    }

    public void Reset()
    {
        _delay = 0;
        _due = 0;
    }
}
