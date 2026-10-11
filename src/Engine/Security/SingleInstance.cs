namespace Micser.Engine.Security;

/// <summary>
/// Ensures one engine per user session.
/// </summary>
public static class SingleInstance
{
    // "Local\" scopes the name to the session; a semaphore (unlike a mutex) can be released from any thread.
    private const string SemaphoreName = @"Local\Micser.Engine";

    /// <summary>
    /// Returns a handle to keep for the process lifetime, or null if another engine is running.
    /// </summary>
    public static IDisposable? TryAcquire()
    {
        if (!OperatingSystem.IsWindows())
        {
            // named semaphores are Windows-only; a lock file in the runtime folder (per user) is released when the process ends
            PrivateFiles.CreateDirectory(EngineOptions.RuntimeDirectory);
            return PrivateFiles.TryOpenExclusive(Path.Combine(EngineOptions.RuntimeDirectory, "engine.lock"));
        }

        var semaphore = new Semaphore(1, 1, SemaphoreName, out var createdNew);
        if (createdNew)
        {
            return semaphore;
        }

        semaphore.Dispose();
        return null;
    }
}
