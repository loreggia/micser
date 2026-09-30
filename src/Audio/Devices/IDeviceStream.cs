namespace Micser.Audio.Devices;

public interface IDeviceStream : IDisposable
{
    int DeviceSampleRate { get; }

    /// <summary>
    /// Set when the device stopped, e.g. because it was removed.
    /// </summary>
    bool IsFaulted { get; }

    ChannelLayout Layout { get; }

    StreamStatistics Statistics { get; }
}
