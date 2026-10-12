using Micser.Audio;
using Micser.Audio.Devices;

namespace Micser.Engine.Tests;

/// <summary>
/// Virtual cables the tests check, instead of cables in the machine's PipeWire.
/// </summary>
internal sealed class FakeVirtualCables : IVirtualCables
{
    public bool IsSupported { get; set; } = true;

    /// <summary>
    /// The layouts of the last <see cref="Set"/>, or null before the first.
    /// </summary>
    public IReadOnlyList<ChannelLayout>? Layouts { get; private set; }

    public void Set(IReadOnlyList<ChannelLayout> layouts)
    {
        if (!IsSupported)
        {
            throw new NotSupportedException();
        }

        Layouts = layouts;
    }
}
