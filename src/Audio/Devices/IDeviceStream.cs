namespace Micser.Audio.Devices;

public interface IDeviceStream : IDisposable
{
    int DeviceSampleRate { get; }

    /// <summary>
    /// Set when the device stopped, e.g. because it was removed, or stopped delivering or taking data, e.g. after the system resumed from
    /// sleep. A faulted stream doesn't recover; it has to be reopened.
    /// </summary>
    bool IsFaulted { get; }

    ChannelLayout Layout { get; }

    StreamStatistics Statistics { get; }
}
