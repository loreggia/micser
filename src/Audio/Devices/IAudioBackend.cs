using Microsoft.Extensions.Logging;

namespace Micser.Audio.Devices;

/// <summary>
/// A platform's audio API: enumerates devices, reports changes and opens device endpoints. <see cref="AudioDeviceService"/> picks one per
/// platform and adds what all backends share (thread pool dispatch of events, the buffering of <see cref="CaptureStream"/> and
/// <see cref="RenderStream"/>).
/// </summary>
internal interface IAudioBackend : IDisposable
{
    /// <summary>
    /// May be raised on a thread that must not call back into the backend, e.g. a COM notification callback.
    /// </summary>
    event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;

    /// <summary>
    /// Raised when the system resumed from sleep or hibernation; may be raised on any thread.
    /// </summary>
    event EventHandler? SystemResumed;

    /// <summary>
    /// The system volume of the default output device. <paramref name="devices"/> raises the backend's events on the thread pool.
    /// </summary>
    IBackendSystemVolume CreateSystemVolume(AudioDeviceService devices, ILogger logger);

    AudioDeviceInfo? GetDefaultDevice(DeviceDirection direction);

    AudioDeviceInfo? GetDevice(string id);

    IReadOnlyList<AudioDeviceInfo> GetDevices(DeviceDirection direction, bool includeInactive);

    /// <param name="loopback">Capture what is played on an output device instead of an input.</param>
    /// <exception cref="InvalidOperationException">The device doesn't exist or isn't active in that direction.</exception>
    ICaptureDevice OpenCapture(string id, bool loopback);

    /// <exception cref="InvalidOperationException">The device doesn't exist or isn't an active output.</exception>
    IRenderDevice OpenRender(string id);
}

/// <summary>
/// A backend's <see cref="ISystemVolume"/>, owned by <see cref="SystemVolume"/>.
/// </summary>
internal interface IBackendSystemVolume : ISystemVolume, IDisposable;

/// <summary>
/// An opened device endpoint, not yet started.
/// </summary>
internal interface IDeviceEndpoint : IDisposable
{
    /// <summary>
    /// The device's channel layout, in which it delivers or takes interleaved samples.
    /// </summary>
    ChannelLayout Layout { get; }

    /// <summary>
    /// The device's name, for logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The duration of the device's periods (the amount it delivers or takes at once), in seconds.
    /// </summary>
    double PeriodSeconds { get; }

    int SampleRate { get; }
}

/// <summary>
/// Receives a capture device's data on its thread.
/// </summary>
internal interface ICaptureHandler
{
    /// <summary>
    /// Interleaved samples in the device's layout.
    /// </summary>
    void OnData(ReadOnlySpan<float> samples);

    /// <summary>
    /// The device delivered this many samples of silence.
    /// </summary>
    void OnSilence(int samples);

    /// <summary>
    /// The device stopped, e.g. because it was removed; it doesn't deliver data anymore.
    /// </summary>
    void OnStopped(Exception? exception);
}

internal interface ICaptureDevice : IDeviceEndpoint
{
    void Start(ICaptureHandler handler);
}

/// <summary>
/// Provides a render device's data on its thread.
/// </summary>
internal interface IRenderHandler
{
    /// <summary>
    /// Fills <paramref name="samples"/> completely with interleaved samples in the device's layout.
    /// </summary>
    void OnRead(Span<float> samples);

    /// <summary>
    /// The device stopped, e.g. because it was removed; it doesn't take data anymore.
    /// </summary>
    void OnStopped(Exception? exception);
}

internal interface IRenderDevice : IDeviceEndpoint
{
    void Start(IRenderHandler handler);
}
