using Micser.Audio;
using Micser.Audio.Devices;
using Microsoft.Extensions.Logging;

namespace Micser.Plugins.Main.Modules;

/// <summary>
/// A module bound to an audio device. The stream is opened once the module is in a graph and reopened when the device
/// changes state, when the system resumed from sleep, and when a watchdog finds it faulted (stopped or stalled). A
/// device that can't be opened is retried with a growing delay. If the device isn't available, the module switches to
/// an active device of the same adapter, e.g. when a USB device is plugged into a different port, and raises
/// <see cref="AudioModule.StateChanged"/>.
/// </summary>
public abstract class DeviceModule<TStream> : AudioModule, IModuleDataSource
    where TStream : class, IDeviceStream
{
    // a stream that ran this long without faulting resets the retry delay
    private static readonly TimeSpan HealthyDuration = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan WatchdogInterval = TimeSpan.FromSeconds(1);

    private readonly AudioDeviceService _devices;
    private readonly RetryBackoff _retry = new();
    private readonly Lock _selectionLock = new();
    private readonly Lock _streamLock = new();
    private readonly Timer _watchdog;
    private bool _isDisposed;
    private TStream? _stream;
    private long _streamOpenedAt;

    protected DeviceModule(AudioDeviceService devices, ILogger logger)
    {
        _devices = devices;
        Logger = logger;
        _watchdog = new Timer(_ => CheckStream());
        _devices.DeviceChanged += OnDeviceChanged;
        _devices.SystemResumed += OnSystemResumed;
    }

    /// <summary>
    /// The adapter of the selected device, used to find the device again when its ID changes.
    /// </summary>
    public string? AdapterName { get; private set; }

    public string? DeviceId { get; private set; }

    /// <summary>
    /// Statistics of the open stream, or null if no device is open.
    /// </summary>
    public StreamStatistics? Statistics
    {
        get
        {
            lock (_streamLock)
            {
                return _stream?.Statistics;
            }
        }
    }

    protected AudioDeviceService Devices => _devices;

    /// <summary>
    /// The direction of the devices this module can use.
    /// </summary>
    protected abstract DeviceDirection Direction { get; }

    protected ILogger Logger { get; }

    public object? GetData()
    {
        return Statistics;
    }

    /// <summary>
    /// Selects a device, or none. Opens it right away when the module is in a graph.
    /// </summary>
    /// <param name="deviceId">The device, or null for none.</param>
    /// <param name="adapterName">The device's adapter, used when the device isn't available (e.g. a restored configuration).</param>
    public void SelectDevice(string? deviceId, string? adapterName = null)
    {
        lock (_selectionLock)
        {
            DeviceId = deviceId;
            AdapterName = deviceId == null ? null : _devices.GetDevice(deviceId)?.AdapterName ?? adapterName;
            _retry.Reset();
            Reopen();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _devices.DeviceChanged -= OnDeviceChanged;
            _devices.SystemResumed -= OnSystemResumed;
            _watchdog.Dispose();
            lock (_selectionLock)
            {
                _isDisposed = true;
                SwapStream(null);
            }
        }

        base.Dispose(disposing);
    }

    protected override void OnAttached()
    {
        lock (_selectionLock)
        {
            _retry.Reset();
            Reopen();
        }

        _watchdog.Change(WatchdogInterval, WatchdogInterval);
    }

    /// <summary>
    /// Called after the stream changed, before it is used by <see cref="UseStream"/>.
    /// </summary>
    protected virtual void OnStreamChanged(TStream? stream)
    {
    }

    protected abstract TStream OpenStream(string deviceId);

    /// <summary>
    /// Runs <paramref name="action"/> with the current stream (null if none) while it can't be replaced.
    /// </summary>
    protected void UseStream<TState>(Action<TStream?, TState> action, TState state)
    {
        lock (_streamLock)
        {
            action(_stream, state);
        }
    }

    /// <summary>
    /// Watchdog: reopens a faulted stream, and retries opening a selected device that isn't open.
    /// </summary>
    private void CheckStream()
    {
        // a reopen in progress (e.g. after a device event) makes this check unnecessary
        if (!_selectionLock.TryEnter())
        {
            return;
        }

        try
        {
            if (_isDisposed || !IsAttached)
            {
                return;
            }

            TStream? stream;
            lock (_streamLock)
            {
                stream = _stream;
            }

            if (stream is { IsFaulted: true })
            {
                if (_retry.IsDue)
                {
                    Logger.LogWarning("The stream of device {Device} stopped; reopening it.", DeviceId);
                    _retry.Attempted();
                    Reopen();
                }
            }
            else if (stream != null)
            {
                if (Environment.TickCount64 - _streamOpenedAt > HealthyDuration.TotalMilliseconds)
                {
                    _retry.Reset();
                }
            }
            else if (DeviceId != null && _retry.IsDue)
            {
                _retry.Attempted();
                Reopen();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Checking the stream of device {Device} failed.", DeviceId);
        }
        finally
        {
            _selectionLock.Exit();
        }
    }

    private void OnDeviceChanged(object? sender, AudioDeviceChangedEventArgs e)
    {
        if (!IsAttached || e.Change == AudioDeviceChange.DefaultChanged)
        {
            return;
        }

        Logger.LogDebug("Device {Device}: {Change}.", e.DeviceId, e.Change);

        lock (_selectionLock)
        {
            if (e.DeviceId == DeviceId)
            {
                _retry.Reset();
                Reopen();
                return;
            }

            var hasStream = false;
            UseStream((stream, _) => hasStream = stream != null, 0);
            if (!hasStream)
            {
                Reopen();
            }
        }
    }

    private void OnSystemResumed(object? sender, EventArgs e)
    {
        lock (_selectionLock)
        {
            if (_isDisposed || !IsAttached || DeviceId == null)
            {
                return;
            }

            // the stream may still look healthy but play or capture nothing
            Logger.LogInformation("Reopening device {Device} after the system resumed.", DeviceId);
            _retry.Reset();
            Reopen();
        }
    }

    private void Reopen()
    {
        if (!IsAttached)
        {
            return;
        }

        TStream? stream = null;
        var device = DeviceId == null ? null : _devices.GetDevice(DeviceId);
        if (device is not { IsActive: true } && AdapterName != null)
        {
            var replacement = _devices.GetDevices(Direction).FirstOrDefault(d => d.AdapterName == AdapterName);
            if (replacement != null)
            {
                Logger.LogInformation("Switching from device {OldDevice} to {NewDevice} of adapter {Adapter}.", DeviceId, replacement.Id, AdapterName);
                DeviceId = replacement.Id;
                device = replacement;
                OnStateChanged();
            }
        }

        if (device is { IsActive: true })
        {
            try
            {
                stream = OpenStream(device.Id);
                Logger.LogInformation("Opened {Device} ({Layout}, {SampleRate} Hz).", device.Name, stream.Layout, stream.DeviceSampleRate);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Could not open device {Device}.", device.Name);
            }
        }

        _streamOpenedAt = Environment.TickCount64;
        SwapStream(stream);
    }

    private void SwapStream(TStream? stream)
    {
        TStream? previous;
        lock (_streamLock)
        {
            previous = _stream;
            _stream = stream;
            OnStreamChanged(stream);
        }

        previous?.Dispose();
    }
}
