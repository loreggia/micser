using Microsoft.Extensions.Logging.Abstractions;
using Micser.Audio.Devices;

namespace Micser.Audio.Tests.Devices;

/// <summary>
/// The streams run their engine side on the audio thread, which must not allocate. The fake devices deliver and take whole 10 ms periods
/// as real ones do, so the fill and with it the drift correction's resampling ratio move from block to block.
/// </summary>
[NotInParallel]
public class DeviceStreamAllocationTests
{
    private static readonly ProcessingFormat Format = new(48000, 240);

    [Test]
    [Arguments(44100)]
    [Arguments(48000)]
    public async Task CaptureStream_Read_DoesNotAllocateAfterWarmUp(int deviceRate)
    {
        var device = new FakeCaptureDevice(deviceRate);
        using var stream = new CaptureStream(device, false, Format, null, NullLogger.Instance);
        var handler = device.Handler!;
        var buffer = new AudioBuffer(Format.FrameCount);
        var period = new float[deviceRate / 100 * 2];
        Array.Fill(period, 0.1f);
        var delivered = 0L;

        void Run(int blocks, ref long block)
        {
            for (var end = block + blocks; block < end; block++)
            {
                // the periods that ended during this block
                while (
                    (delivered + (deviceRate / 100)) * Format.SampleRate <= (block + 1) * Format.FrameCount * deviceRate
                )
                {
                    handler.OnData(period);
                    delivered += deviceRate / 100;
                }

                stream.Read(buffer);
            }
        }

        long position = 0;
        Run(2000, ref position);
        var allocated = Allocations.Measure(() => Run(2000, ref position));

        await Assert.That(buffer.Layout).IsEqualTo(ChannelLayout.Stereo);
        await Assert.That(allocated.Bytes).IsEqualTo(0).Because(allocated.ToString());
    }

    [Test]
    [Arguments(44100)]
    [Arguments(48000)]
    public async Task RenderStream_Write_DoesNotAllocateAfterWarmUp(int deviceRate)
    {
        var device = new FakeRenderDevice(deviceRate);
        using var stream = new RenderStream(device, Format, null, NullLogger.Instance);
        var handler = device.Handler!;
        var buffer = new AudioBuffer(Format.FrameCount, ChannelLayout.Stereo);
        buffer.GetChannel(0).Fill(0.1f);
        buffer.GetChannel(1).Fill(-0.1f);
        var period = new float[deviceRate / 100 * 2];
        var taken = 0L;

        void Run(int blocks, ref long block)
        {
            for (var end = block + blocks; block < end; block++)
            {
                stream.Write(buffer);
                while ((taken + (deviceRate / 100)) * Format.SampleRate <= (block + 1) * Format.FrameCount * deviceRate)
                {
                    handler.OnRead(period);
                    taken += deviceRate / 100;
                }
            }
        }

        long position = 0;
        Run(2000, ref position);
        var allocated = Allocations.Measure(() => Run(2000, ref position));

        await Assert.That(allocated.Bytes).IsEqualTo(0).Because(allocated.ToString());
    }

    private abstract class FakeDevice : IDeviceEndpoint
    {
        protected FakeDevice(int sampleRate)
        {
            SampleRate = sampleRate;
        }

        public ChannelLayout Layout => ChannelLayout.Stereo;

        public string Name => "Test";

        public double PeriodSeconds => 0.01;

        public int SampleRate { get; }

        public void Dispose() { }
    }

    private sealed class FakeCaptureDevice : FakeDevice, ICaptureDevice
    {
        public FakeCaptureDevice(int sampleRate)
            : base(sampleRate) { }

        public ICaptureHandler? Handler { get; private set; }

        public void Start(ICaptureHandler handler)
        {
            Handler = handler;
        }
    }

    private sealed class FakeRenderDevice : FakeDevice, IRenderDevice
    {
        public FakeRenderDevice(int sampleRate)
            : base(sampleRate) { }

        public IRenderHandler? Handler { get; private set; }

        public void Start(IRenderHandler handler)
        {
            Handler = handler;
        }
    }
}
