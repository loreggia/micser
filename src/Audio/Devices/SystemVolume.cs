using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.CoreAudioApi;

namespace Micser.Audio.Devices;

/// <param name="Gain">The linear gain the device applies (from its level in dB), 0..1.</param>
/// <param name="IsMuted">Whether the device is muted.</param>
public readonly record struct SystemVolumeLevel(float Gain, bool IsMuted);

/// <summary>
/// The volume of the default output device, which Windows' volume slider and keys control.
/// </summary>
public interface ISystemVolume
{
    /// <summary>
    /// Raised on a thread pool thread when <see cref="Level"/> changes, also when another device becomes the default.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// The current level, or null if there is no default output device.
    /// </summary>
    SystemVolumeLevel? Level { get; }
}

/// <summary>
/// Follows the volume of the default output device through its endpoint volume notifications.
/// </summary>
public sealed class SystemVolume : ISystemVolume, IDisposable
{
    private readonly AudioDeviceService _devices;
    private readonly Lock _lock = new();
    private readonly ILogger _logger;
    private MMDevice? _device;
    private SystemVolumeLevel? _level;

    public SystemVolume(AudioDeviceService devices, ILogger<SystemVolume>? logger = null)
    {
        _devices = devices;
        _logger = logger ?? NullLogger<SystemVolume>.Instance;
        _devices.DeviceChanged += OnDeviceChanged;
        Attach();
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
        _devices.DeviceChanged -= OnDeviceChanged;
        lock (_lock)
        {
            Detach();
        }
    }

    private void Attach()
    {
        lock (_lock)
        {
            Detach();
            _device = _devices.TryGetDefaultRenderDevice();
            if (_device != null)
            {
                try
                {
                    _device.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Can't follow the volume of {Device}.", _device.FriendlyName);
                }
            }
        }

        Update();
    }

    private void Detach()
    {
        if (_device != null)
        {
            _device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
            _device.Dispose();
            _device = null;
        }
    }

    private void OnDeviceChanged(object? sender, AudioDeviceChangedEventArgs e)
    {
        if (e.Change == AudioDeviceChange.DefaultChanged)
        {
            Attach();
        }
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data)
    {
        // COM notification callbacks must not call back into the device API
        ThreadPool.QueueUserWorkItem(_ => Update());
    }

    private void Update()
    {
        SystemVolumeLevel? level = null;
        bool changed;
        lock (_lock)
        {
            if (_device != null)
            {
                try
                {
                    var volume = _device.AudioEndpointVolume;
                    var gain = volume.MasterVolumeLevelScalar > 0 ? Decibels.ToLinear(volume.MasterVolumeLevel) : 0f;
                    level = new SystemVolumeLevel(Math.Clamp(gain, 0f, 1f), volume.Mute);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Reading the system volume failed.");
                }
            }

            changed = level != _level;
            _level = level;
        }

        if (changed)
        {
            try
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A system volume handler failed.");
            }
        }
    }
}
