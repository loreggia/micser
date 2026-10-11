using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Micser.Audio.Devices;

/// <param name="Gain">The linear gain the device applies (from its level in dB), 0..1.</param>
/// <param name="IsMuted">Whether the device is muted.</param>
public readonly record struct SystemVolumeLevel(float Gain, bool IsMuted);

/// <summary>
/// The volume of the default output device, which the system's volume slider and keys control.
/// </summary>
public interface ISystemVolume
{
    /// <summary>
    /// Raised on a thread pool thread when <see cref="Level"/> changes, also when another device becomes the default.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// The current level, or null if there is no default output device or the platform's backend can't read it.
    /// </summary>
    SystemVolumeLevel? Level { get; }
}

/// <summary>
/// Follows the volume of the default output device through the audio backend of <see cref="AudioDeviceService"/>.
/// </summary>
public sealed class SystemVolume : ISystemVolume, IDisposable
{
    private readonly IBackendSystemVolume _volume;

    public SystemVolume(AudioDeviceService devices, ILogger<SystemVolume>? logger = null)
    {
        _volume = devices.Backend.CreateSystemVolume(devices, logger ?? NullLogger<SystemVolume>.Instance);
    }

    public event EventHandler? Changed
    {
        add => _volume.Changed += value;
        remove => _volume.Changed -= value;
    }

    public SystemVolumeLevel? Level => _volume.Level;

    public void Dispose()
    {
        _volume.Dispose();
    }
}
