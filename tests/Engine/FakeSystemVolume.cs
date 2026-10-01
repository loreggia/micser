using Micser.Audio.Devices;

namespace Micser.Engine.Tests;

/// <summary>
/// A system volume the tests set, instead of the volume of the machine's default output device.
/// </summary>
internal sealed class FakeSystemVolume : ISystemVolume
{
    public event EventHandler? Changed;

    public SystemVolumeLevel? Level { get; private set; } = new(1f, false);

    public void Set(SystemVolumeLevel? level)
    {
        Level = level;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
