namespace Micser.Audio.Devices;

public enum AudioDeviceChange
{
    Added,
    Removed,
    StateChanged,
    DefaultChanged,
}

public enum DeviceDirection
{
    Input,
    Output,
}

/// <param name="Id">The endpoint ID; changes when a device is plugged into a different port.</param>
/// <param name="Name">The endpoint's full name, e.g. "Speakers (Realtek Audio)".</param>
/// <param name="Description">The endpoint's short name, e.g. "Speakers".</param>
/// <param name="AdapterName">The name of the audio adapter, e.g. "Realtek Audio". Stable across ports.</param>
/// <param name="Layout">The shared-mode channel layout, or null if the device isn't active.</param>
/// <param name="SampleRate">The shared-mode sample rate, or null if the device isn't active.</param>
public sealed record AudioDeviceInfo(
    string Id,
    string Name,
    string? Description,
    string? AdapterName,
    DeviceDirection Direction,
    bool IsActive,
    ChannelLayout? Layout,
    int? SampleRate
);

public sealed class AudioDeviceChangedEventArgs : EventArgs
{
    public AudioDeviceChangedEventArgs(string deviceId, AudioDeviceChange change)
    {
        DeviceId = deviceId;
        Change = change;
    }

    public AudioDeviceChange Change { get; }

    public string DeviceId { get; }
}
