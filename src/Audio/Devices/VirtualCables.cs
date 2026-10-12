namespace Micser.Audio.Devices;

/// <summary>
/// The virtual audio cables that the audio backend creates itself (Linux with PipeWire). On Windows a driver provides them, which the
/// shell manages.
/// </summary>
public interface IVirtualCables
{
    bool IsSupported { get; }

    /// <summary>
    /// Creates, changes and removes cables to match <paramref name="layouts"/> (cable 1 first). Each cable is an output device that
    /// apps play to ("Micser Cable N Input") and an input device that they record from ("Micser Cable N Output").
    /// </summary>
    /// <exception cref="NotSupportedException">The backend doesn't create cables (<see cref="IsSupported"/>).</exception>
    void Set(IReadOnlyList<ChannelLayout> layouts);
}

/// <summary>
/// The cables of <see cref="AudioDeviceService"/>'s backend.
/// </summary>
public sealed class VirtualCables : IVirtualCables
{
    /// <summary>
    /// The most cables, as with the Windows driver.
    /// </summary>
    public const int MaxCount = 16;

    private readonly AudioDeviceService _devices;

    public VirtualCables(AudioDeviceService devices)
    {
        _devices = devices;
    }

    public bool IsSupported => _devices.Backend.SupportsCables;

    public void Set(IReadOnlyList<ChannelLayout> layouts)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(layouts.Count, MaxCount, nameof(layouts));
        _devices.Backend.SetCables(layouts);
    }
}
