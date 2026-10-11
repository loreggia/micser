using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Micser.Audio.Devices.Wasapi;

namespace Micser.Audio.Devices;

/// <summary>
/// Enumerates audio endpoints, reports changes and opens streams on them, through the platform's audio backend (WASAPI on Windows; none
/// elsewhere yet, so there are no devices).
/// </summary>
public sealed class AudioDeviceService : IDisposable
{
    private readonly ILogger _logger;

    public AudioDeviceService(ILogger<AudioDeviceService>? logger = null)
    {
        _logger = logger ?? NullLogger<AudioDeviceService>.Instance;
        if (OperatingSystem.IsWindows())
        {
            Backend = new WasapiBackend(_logger);
        }
        else
        {
            _logger.LogWarning("There is no audio backend for this platform yet; no audio devices are available.");
            Backend = new NullAudioBackend();
        }

        Backend.DeviceChanged += OnDeviceChanged;
        Backend.SystemResumed += OnSystemResumed;
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

    internal IAudioBackend Backend { get; }

    public void Dispose()
    {
        Backend.DeviceChanged -= OnDeviceChanged;
        Backend.SystemResumed -= OnSystemResumed;
        Backend.Dispose();
    }

    public AudioDeviceInfo? GetDefaultDevice(DeviceDirection direction)
    {
        return Backend.GetDefaultDevice(direction);
    }

    public AudioDeviceInfo? GetDevice(string id)
    {
        return Backend.GetDevice(id);
    }

    public IReadOnlyList<AudioDeviceInfo> GetDevices(DeviceDirection direction, bool includeInactive = false)
    {
        return Backend.GetDevices(direction, includeInactive);
    }

    /// <param name="initialTargetMilliseconds">The buffer to start with, e.g. one the stream learned before; null for the minimum.</param>
    /// <exception cref="InvalidOperationException">The device doesn't exist or isn't an active input.</exception>
    public CaptureStream OpenCapture(string deviceId, ProcessingFormat format, double? initialTargetMilliseconds = null)
    {
        return OpenCaptureStream(deviceId, false, format, initialTargetMilliseconds);
    }

    /// <summary>
    /// Captures what is played on an output device.
    /// </summary>
    /// <param name="initialTargetMilliseconds">The buffer to start with, e.g. one the stream learned before; null for the minimum.</param>
    /// <exception cref="InvalidOperationException">The device doesn't exist or isn't an active output.</exception>
    public CaptureStream OpenLoopback(
        string deviceId,
        ProcessingFormat format,
        double? initialTargetMilliseconds = null
    )
    {
        return OpenCaptureStream(deviceId, true, format, initialTargetMilliseconds);
    }

    /// <param name="initialTargetMilliseconds">The buffer to start with, e.g. one the stream learned before; null for the minimum.</param>
    /// <exception cref="InvalidOperationException">The device doesn't exist or isn't an active output.</exception>
    public RenderStream OpenRender(string deviceId, ProcessingFormat format, double? initialTargetMilliseconds = null)
    {
        var device = Backend.OpenRender(deviceId);
        try
        {
            return new RenderStream(device, format, initialTargetMilliseconds, _logger);
        }
        catch
        {
            device.Dispose();
            throw;
        }
    }

    private void OnDeviceChanged(object? sender, AudioDeviceChangedEventArgs e)
    {
        // backends may notify on threads that must not call back into the device API (COM callbacks), so handlers run on the thread pool
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                DeviceChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A device change handler failed.");
            }
        });
    }

    private void OnSystemResumed(object? sender, EventArgs e)
    {
        ThreadPool.QueueUserWorkItem(_ =>
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
        });
    }

    private CaptureStream OpenCaptureStream(
        string deviceId,
        bool loopback,
        ProcessingFormat format,
        double? initialTargetMilliseconds
    )
    {
        var device = Backend.OpenCapture(deviceId, loopback);
        try
        {
            return new CaptureStream(device, loopback, format, initialTargetMilliseconds, _logger);
        }
        catch
        {
            device.Dispose();
            throw;
        }
    }
}
