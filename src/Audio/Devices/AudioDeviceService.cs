using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.CoreAudioApi;

namespace Micser.Audio.Devices;

/// <summary>
/// Enumerates audio endpoints, reports changes and opens streams on them.
/// </summary>
public sealed class AudioDeviceService : IDisposable
{
    private static readonly PropertyKey DeviceDescriptionKey = new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 2);

    private readonly MMDeviceEnumerator _enumerator;
    private readonly ILogger _logger;
    private readonly MMDeviceNotificationClient _notificationClient;
    private readonly ResumeNotification? _resumeNotification;

    public AudioDeviceService(ILogger<AudioDeviceService>? logger = null)
    {
        _logger = logger ?? NullLogger<AudioDeviceService>.Instance;
        _enumerator = new MMDeviceEnumerator();
        _notificationClient = _enumerator.CreateNotificationClient(false);
        _notificationClient.DeviceAdded += (_, e) => Raise(e.DeviceId, AudioDeviceChange.Added);
        _notificationClient.DeviceRemoved += (_, e) => Raise(e.DeviceId, AudioDeviceChange.Removed);
        _notificationClient.DeviceStateChanged += (_, e) => Raise(e.DeviceId, AudioDeviceChange.StateChanged);
        _notificationClient.DefaultDeviceChanged += (_, e) => Raise(e.DeviceId, AudioDeviceChange.DefaultChanged);

        try
        {
            _resumeNotification = new ResumeNotification(OnSystemResumed);
        }
        catch (Win32Exception ex)
        {
            _logger.LogWarning(ex, "Resume notifications are unavailable; streams are reopened only when they stop.");
        }
    }

    /// <summary>
    /// Raised on a thread pool thread.
    /// </summary>
    public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;

    /// <summary>
    /// Raised on a thread pool thread when the system resumed from sleep or hibernation. Device streams may still look healthy then but
    /// play or capture nothing, so they should be reopened.
    /// </summary>
    public event EventHandler? SystemResumed;

    public void Dispose()
    {
        _resumeNotification?.Dispose();
        _notificationClient.Dispose();
        _enumerator.Dispose();
    }

    public AudioDeviceInfo? GetDefaultDevice(DeviceDirection direction)
    {
        if (!_enumerator.TryGetDefaultAudioEndpoint(ToDataFlow(direction), Role.Multimedia, out var device))
        {
            return null;
        }

        using (device)
        {
            return Describe(device);
        }
    }

    public AudioDeviceInfo? GetDevice(string id)
    {
        using var device = TryGetDevice(id);
        return device == null ? null : Describe(device);
    }

    public IReadOnlyList<AudioDeviceInfo> GetDevices(DeviceDirection direction, bool includeInactive = false)
    {
        var state = includeInactive ? DeviceState.Active | DeviceState.Disabled | DeviceState.Unplugged : DeviceState.Active;
        using var devices = _enumerator.EnumerateAudioEndPoints(ToDataFlow(direction), state);

        var result = new List<AudioDeviceInfo>(devices.Count);
        foreach (var device in devices)
        {
            using (device)
            {
                result.Add(Describe(device));
            }
        }

        return result;
    }

    /// <exception cref="InvalidOperationException">The device doesn't exist or isn't an active input.</exception>
    public CaptureStream OpenCapture(string deviceId, ProcessingFormat format)
    {
        return new CaptureStream(GetActiveDevice(deviceId, DataFlow.Capture), false, format, _logger);
    }

    /// <summary>
    /// Captures what is played on an output device.
    /// </summary>
    /// <exception cref="InvalidOperationException">The device doesn't exist or isn't an active output.</exception>
    public CaptureStream OpenLoopback(string deviceId, ProcessingFormat format)
    {
        return new CaptureStream(GetActiveDevice(deviceId, DataFlow.Render), true, format, _logger);
    }

    /// <exception cref="InvalidOperationException">The device doesn't exist or isn't an active output.</exception>
    public RenderStream OpenRender(string deviceId, ProcessingFormat format)
    {
        return new RenderStream(GetActiveDevice(deviceId, DataFlow.Render), format, _logger);
    }

    private static DataFlow ToDataFlow(DeviceDirection direction)
    {
        return direction == DeviceDirection.Input ? DataFlow.Capture : DataFlow.Render;
    }

    private AudioDeviceInfo Describe(MMDevice device)
    {
        var isActive = device.State == DeviceState.Active;
        ChannelLayout? layout = null;
        int? sampleRate = null;

        if (isActive)
        {
            try
            {
                using var client = device.CreateAudioClient();
                var mixFormat = client.MixFormat;
                layout = ChannelLayout.FromWaveFormat(mixFormat);
                sampleRate = mixFormat.SampleRate;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read the mix format of {Device}.", device.ID);
            }
        }

        return new AudioDeviceInfo(
            device.ID,
            device.FriendlyName,
            TryGetString(device, DeviceDescriptionKey),
            device.DeviceFriendlyName,
            device.DataFlow == DataFlow.Capture ? DeviceDirection.Input : DeviceDirection.Output,
            isActive,
            layout,
            sampleRate);
    }

    private MMDevice GetActiveDevice(string id, DataFlow dataFlow)
    {
        var device = TryGetDevice(id) ?? throw new InvalidOperationException($"Audio device '{id}' not found.");
        if (device.DataFlow != dataFlow || device.State != DeviceState.Active)
        {
            device.Dispose();
            throw new InvalidOperationException($"Audio device '{id}' is not an active {(dataFlow == DataFlow.Capture ? "input" : "output")}.");
        }

        return device;
    }

    private void OnSystemResumed()
    {
        _logger.LogInformation("The system resumed.");
        try
        {
            SystemResumed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A resume handler failed.");
        }
    }

    private void Raise(string deviceId, AudioDeviceChange change)
    {
        // COM notification callbacks must not call back into the device API, so handlers run on the thread pool.
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                DeviceChanged?.Invoke(this, new AudioDeviceChangedEventArgs(deviceId, change));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A device change handler failed.");
            }
        });
    }

    private MMDevice? TryGetDevice(string id)
    {
        try
        {
            return _enumerator.GetDevice(id);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Audio device {Device} not found.", id);
            return null;
        }
    }

    private string? TryGetString(MMDevice device, PropertyKey key)
    {
        try
        {
            return device.Properties.TryGetValue<string>(key, out var value) ? value : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read a property of {Device}.", device.ID);
            return null;
        }
    }
}
