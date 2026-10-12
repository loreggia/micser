using Microsoft.Extensions.Logging;

namespace Micser.Audio.Devices;

/// <summary>
/// A backend without devices, for platforms without an audio backend yet (Linux until the PipeWire backend). Device modules then find
/// no device to open; everything else works.
/// </summary>
internal sealed class NullAudioBackend : IAudioBackend
{
    public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged
    {
        add { }
        remove { }
    }

    public event EventHandler? SystemResumed
    {
        add { }
        remove { }
    }

    public IBackendSystemVolume CreateSystemVolume(AudioDeviceService devices, ILogger logger)
    {
        return new NullSystemVolume();
    }

    public void Dispose() { }

    public AudioDeviceInfo? GetDefaultDevice(DeviceDirection direction)
    {
        return null;
    }

    public AudioDeviceInfo? GetDevice(string id)
    {
        return null;
    }

    public bool SupportsCables => false;

    public IReadOnlyList<AudioDeviceInfo> GetDevices(DeviceDirection direction, bool includeInactive)
    {
        return [];
    }

    public void SetCables(IReadOnlyList<ChannelLayout> layouts)
    {
        throw new NotSupportedException("There are no virtual cables without an audio backend.");
    }

    public ICaptureDevice OpenCapture(string id, bool loopback)
    {
        throw new InvalidOperationException($"Audio device '{id}' not found.");
    }

    public IRenderDevice OpenRender(string id)
    {
        throw new InvalidOperationException($"Audio device '{id}' not found.");
    }

    private sealed class NullSystemVolume : IBackendSystemVolume
    {
        public event EventHandler? Changed
        {
            add { }
            remove { }
        }

        public SystemVolumeLevel? Level => null;

        public void Dispose() { }
    }
}
