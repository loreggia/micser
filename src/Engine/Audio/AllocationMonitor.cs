using Micser.Audio;

namespace Micser.Engine.Audio;

/// <param name="Name">The module's name in the UI, if it has one.</param>
/// <param name="Bytes">The bytes the module allocated on the audio thread so far.</param>
public sealed record ModuleAllocations(Guid Id, string Type, string? Name, long Bytes);

/// <summary>
/// Logs what the audio thread allocated, in total and per module, every <see cref="Interval"/> in which it allocated. Runs only with
/// <see cref="EngineOptions.TrackAllocations"/>.
/// </summary>
public sealed class AllocationMonitor : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private readonly AudioHost _host;
    private readonly ILogger<AllocationMonitor> _logger;

    public AllocationMonitor(AudioHost host, ILogger<AllocationMonitor> logger)
    {
        _host = host;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tracking the allocations of the audio thread.");
        var previousEngine = new EngineStatistics();
        var previousModules = new Dictionary<Guid, long>();
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var (engine, modules) = _host.GetAllocations();

                // a restarted engine (e.g. new settings) counts from 0 again
                var restarted = engine.Blocks < previousEngine.Blocks;
                var bytes = engine.AllocatedBytes - (restarted ? 0 : previousEngine.AllocatedBytes);
                var blocks = engine.AllocatingBlocks - (restarted ? 0 : previousEngine.AllocatingBlocks);
                previousEngine = engine;

                var allocating = modules
                    .Select(m => (Module: m, Bytes: m.Bytes - previousModules.GetValueOrDefault(m.Id)))
                    .Where(m => m.Bytes > 0)
                    .OrderByDescending(m => m.Bytes)
                    .Select(m =>
                        string.IsNullOrEmpty(m.Module.Name)
                            ? $"{m.Module.Type} ({m.Module.Id}): {m.Bytes:N0} B"
                            : $"{m.Module.Type} \"{m.Module.Name}\" ({m.Module.Id}): {m.Bytes:N0} B"
                    )
                    .ToArray();
                previousModules = modules.ToDictionary(m => m.Id, m => m.Bytes);

                if (bytes > 0 || allocating.Length > 0)
                {
                    _logger.LogWarning(
                        "The audio thread allocated {Bytes:N0} B in {Blocks} blocks within {Interval}; modules: {Modules}",
                        bytes,
                        blocks,
                        Interval,
                        allocating.Length > 0 ? string.Join(", ", allocating) : "none"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reading the allocations of the audio thread failed.");
            }
        }
    }
}
