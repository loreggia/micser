using System.ComponentModel;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;

namespace Micser.Audio.Devices.Wasapi;

/// <summary>
/// Windows audio endpoints through WASAPI (NAudio), in shared mode.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WasapiBackend : IAudioBackend
{
    private static readonly PropertyKey DeviceDescriptionKey = new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 2);

    private readonly MMDeviceEnumerator _enumerator;
    private readonly ILogger _logger;
    private readonly MMDeviceNotificationClient _notificationClient;
    private readonly ResumeNotification? _resumeNotification;

    public WasapiBackend(ILogger logger)
    {
        _logger = logger;
        _enumerator = new MMDeviceEnumerator();
        _notificationClient = _enumerator.CreateNotificationClient(false);
        _notificationClient.DeviceAdded += (_, e) => Raise(e.DeviceId, AudioDeviceChange.Added);
        _notificationClient.DeviceRemoved += (_, e) => Raise(e.DeviceId, AudioDeviceChange.Removed);
        _notificationClient.DeviceStateChanged += (_, e) => Raise(e.DeviceId, AudioDeviceChange.StateChanged);
        _notificationClient.DefaultDeviceChanged += (_, e) => Raise(e.DeviceId, AudioDeviceChange.DefaultChanged);

        try
        {
            _resumeNotification = new ResumeNotification(() => SystemResumed?.Invoke(this, EventArgs.Empty));
        }
        catch (Win32Exception ex)
        {
            _logger.LogWarning(ex, "Resume notifications are unavailable; streams are reopened only when they stop.");
        }
    }

    public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;

    public event EventHandler? SystemResumed;

    /// <summary>
    /// The VAC driver provides the cables on Windows; the shell installs and changes it.
    /// </summary>
    public bool SupportsCables => false;

    public IBackendSystemVolume CreateSystemVolume(AudioDeviceService devices, ILogger logger)
    {
        return new WasapiSystemVolume(this, devices, logger);
    }

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

    public IReadOnlyList<AudioDeviceInfo> GetDevices(DeviceDirection direction, bool includeInactive)
    {
        var state = includeInactive
            ? DeviceState.Active | DeviceState.Disabled | DeviceState.Unplugged
            : DeviceState.Active;
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

    public ICaptureDevice OpenCapture(string id, bool loopback)
    {
        return new WasapiCaptureDevice(
            GetActiveDevice(id, loopback ? DataFlow.Render : DataFlow.Capture),
            loopback,
            _logger
        );
    }

    public void SetCables(IReadOnlyList<ChannelLayout> layouts)
    {
        throw new NotSupportedException("On Windows, the virtual cables come from the driver.");
    }

    public IRenderDevice OpenRender(string id)
    {
        return new WasapiRenderDevice(GetActiveDevice(id, DataFlow.Render), _logger);
    }

    /// <summary>
    /// The default output device, which the caller disposes, or null if there is none.
    /// </summary>
    internal MMDevice? TryGetDefaultRenderDevice()
    {
        return _enumerator.TryGetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out var device) ? device : null;
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
            sampleRate
        );
    }

    private MMDevice GetActiveDevice(string id, DataFlow dataFlow)
    {
        var device = TryGetDevice(id) ?? throw new InvalidOperationException($"Audio device '{id}' not found.");
        if (device.DataFlow != dataFlow || device.State != DeviceState.Active)
        {
            device.Dispose();
            throw new InvalidOperationException(
                $"Audio device '{id}' is not an active {(dataFlow == DataFlow.Capture ? "input" : "output")}."
            );
        }

        return device;
    }

    private void Raise(string deviceId, AudioDeviceChange change)
    {
        DeviceChanged?.Invoke(this, new AudioDeviceChangedEventArgs(deviceId, change));
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
