using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Micser.Audio.Devices.PipeWire;

/// <summary>
/// Follows the volume and mute of the default sink (its <c>Props</c>: <c>channelVolumes</c>, linear, and <c>mute</c>), which the desktop's
/// volume control and keys set.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class PipeWireSystemVolume : IBackendSystemVolume
{
    private readonly PipeWireBackend _backend;
    private readonly Lock _lock = new();
    private readonly ILogger _logger;
    private SystemVolumeLevel? _level;

    public PipeWireSystemVolume(PipeWireBackend backend, ILogger logger)
    {
        _backend = backend;
        _logger = logger;
        _level = backend.GetDefaultVolume();
        backend.DefaultVolumeChanged += OnDefaultVolumeChanged;
    }

    public event EventHandler? Changed;

    public SystemVolumeLevel? Level
    {
        get
        {
            lock (_lock)
            {
                return _level;
            }
        }
    }

    public void Dispose()
    {
        _backend.DefaultVolumeChanged -= OnDefaultVolumeChanged;
    }

    private void OnDefaultVolumeChanged(object? sender, EventArgs e)
    {
        var level = _backend.GetDefaultVolume();
        lock (_lock)
        {
            if (level == _level)
            {
                return;
            }

            _level = level;
        }

        _logger.LogDebug("System volume: {Level}.", level);

        // raised on the PipeWire thread, which handlers must not block
        ThreadPool.QueueUserWorkItem(_ => Changed?.Invoke(this, EventArgs.Empty));
    }
}
