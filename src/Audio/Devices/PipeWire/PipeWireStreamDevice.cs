using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using static Micser.Audio.Devices.PipeWire.PipeWireNative;

namespace Micser.Audio.Devices.PipeWire;

/// <summary>
/// A <c>pw_stream</c> connected to a sink or source, in 32-bit float at the graph's rate and in the layout's channel order, so PipeWire
/// only reorders the device's channels. Its <c>process</c> callback runs on PipeWire's realtime data thread
/// (<c>PW_STREAM_FLAG_RT_PROCESS</c>). It doesn't follow another device when its own goes away (<c>node.dont-reconnect</c>); it faults
/// instead, as WASAPI streams do.
/// </summary>
[SupportedOSPlatform("linux")]
internal abstract unsafe class PipeWireStreamDevice : IDeviceEndpoint
{
    private static readonly StreamEvents* EventTable = CreateEvents();

    private readonly PipeWireBackend _backend;
    private GCHandle _handle;
    private SpaHook* _hook;
    private int _isDisposed;
    private volatile string? _fault;
    private nint _stream;

    protected PipeWireStreamDevice(
        PipeWireBackend backend,
        StreamTarget target,
        int direction,
        string category,
        IEnumerable<KeyValuePair<string, string>> properties
    )
    {
        _backend = backend;
        Target = target;
        Layout = target.Layout;
        Name = target.Description;
        SampleRate = target.Rate;
        PeriodSeconds = (double)PipeWireBackend.PeriodFrames / target.Rate;
        ChannelCount = target.Positions.Length;
        _handle = GCHandle.Alloc(this);
        _hook = (SpaHook*)NativeMemory.AllocZeroed((nuint)sizeof(SpaHook));

        List<KeyValuePair<string, string>> streamProperties =
        [
            new("media.type", "Audio"),
            new("media.category", category),
            new("node.name", "micser"),
            new("node.description", "Micser"),
            new("target.object", target.Name),
            new("node.latency", $"{PipeWireBackend.PeriodFrames}/{target.Rate}"),
            new("node.dont-reconnect", "true"),
            .. properties,
        ];

        var format = SpaPod.AudioFormat(ParamEnumFormat, target.Rate, target.Positions);
        backend.Lock();
        try
        {
            if (!backend.Register(this))
            {
                throw new InvalidOperationException($"Audio device '{target.Name}' not found.");
            }

            fixed (byte* name = "Micser"u8)
            {
                _stream = StreamNew(backend.Core, name, NewProperties(streamProperties));
            }

            if (_stream == 0)
            {
                throw new InvalidOperationException($"Could not create a PipeWire stream for '{target.Name}'.");
            }

            StreamAddListener(_stream, _hook, (nint)EventTable, GCHandle.ToIntPtr(_handle));
            fixed (byte* pod = format)
            {
                var parameters = (nint)pod;
                var result = StreamConnect(
                    _stream,
                    direction,
                    IdAny,
                    StreamFlagAutoconnect | StreamFlagMapBuffers | StreamFlagRtProcess | StreamFlagDontReconnect,
                    &parameters,
                    1
                );
                if (result < 0)
                {
                    throw new InvalidOperationException(
                        $"Could not connect a PipeWire stream to '{target.Name}' (error {result})."
                    );
                }
            }
        }
        catch
        {
            backend.Unlock();
            Dispose();
            throw;
        }

        backend.Unlock();
    }

    public ChannelLayout Layout { get; }

    public string Name { get; }

    public double PeriodSeconds { get; }

    public int SampleRate { get; }

    public StreamTarget Target { get; }

    /// <summary>
    /// The channels of the stream's buffers.
    /// </summary>
    protected int ChannelCount { get; }

    protected ILogger Logger => _backend.Logger;

    public void Dispose()
    {
        // the backend disposes its streams before itself, so a later Dispose (by the stream's owner) must not touch PipeWire
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
        {
            return;
        }

        _backend.Lock();
        try
        {
            DestroyStream();
        }
        finally
        {
            _backend.Unlock();
        }

        _backend.Unregister(this);
        NativeMemory.Free(_hook);
        _hook = null;
        _handle.Free();
    }

    /// <summary>
    /// Destroys the native stream, on disposal or when the connection goes away (after <see cref="Fault"/>). Loop lock held.
    /// </summary>
    public void DestroyStream()
    {
        if (_stream == 0)
        {
            return;
        }

        // no process callback runs after pw_stream_destroy returns
        HookRemove(_hook);
        StreamDestroy(_stream);
        _stream = 0;
    }

    /// <summary>
    /// The stream stopped for good, e.g. because its device was removed. Called on the PipeWire thread.
    /// </summary>
    public void Fault(string reason)
    {
        if (_fault != null)
        {
            return;
        }

        _fault = reason;
        OnFault(new InvalidOperationException($"{Name}: {reason}."));
    }

    /// <summary>
    /// The exception of a fault before the handler was there, which <see cref="OnFault"/> then reports when it starts.
    /// </summary>
    protected Exception? PendingFault => _fault == null ? null : new InvalidOperationException($"{Name}: {_fault}.");

    /// <summary>
    /// Reports a fault to the handler, if there is one yet.
    /// </summary>
    protected abstract void OnFault(Exception exception);

    /// <summary>
    /// Dequeues, fills or drains and queues a buffer. Realtime data thread: no allocations, locks or logging.
    /// </summary>
    protected abstract void Process(nint stream);

    private static StreamEvents* CreateEvents()
    {
        var events = (StreamEvents*)NativeMemory.AllocZeroed((nuint)sizeof(StreamEvents));
        events->Version = 2;
        events->StateChanged = &OnStateChanged;
        events->Process = &OnProcess;
        return events;
    }

    [UnmanagedCallersOnly]
    private static void OnProcess(nint data)
    {
        if (GCHandle.FromIntPtr(data).Target is PipeWireStreamDevice device && device._stream != 0)
        {
            try
            {
                device.Process(device._stream);
            }
            catch
            {
                // an exception must not reach PipeWire's thread; the stream's statistics show the gap
            }
        }
    }

    [UnmanagedCallersOnly]
    private static void OnStateChanged(nint data, int oldState, int state, byte* error)
    {
        if (GCHandle.FromIntPtr(data).Target is not PipeWireStreamDevice device)
        {
            return;
        }

        try
        {
            if (state == StreamStateError || (state == StreamStateUnconnected && oldState != StreamStateUnconnected))
            {
                device.Fault(Utf8ToString(error) ?? "the stream was disconnected");
            }
            else
            {
                device.Logger.LogDebug("PipeWire stream to {Device}: state {State}.", device.Name, state);
            }
        }
        catch (Exception ex)
        {
            device.Logger.LogError(ex, "A PipeWire stream event handler failed.");
        }
    }
}

/// <summary>
/// Captures a source, or a sink's monitor (<c>stream.capture.sink</c>).
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed unsafe class PipeWireCaptureDevice : PipeWireStreamDevice, ICaptureDevice
{
    private volatile ICaptureHandler? _handler;

    public PipeWireCaptureDevice(PipeWireBackend backend, StreamTarget target, bool loopback)
        : base(
            backend,
            target,
            DirectionInput,
            "Capture",
            loopback ? [new KeyValuePair<string, string>("stream.capture.sink", "true")] : []
        )
    {
        backend.Logger.LogInformation(
            "Capture from {Device}{Loopback}: {Layout}, {Rate} Hz.",
            target.Description,
            loopback ? " (loopback)" : "",
            target.Layout,
            target.Rate
        );
    }

    public void Start(ICaptureHandler handler)
    {
        _handler = handler;
        if (PendingFault is { } fault)
        {
            handler.OnStopped(fault);
        }
    }

    protected override void OnFault(Exception exception)
    {
        _handler?.OnStopped(exception);
    }

    protected override void Process(nint stream)
    {
        var buffer = StreamDequeueBuffer(stream);
        if (buffer == null)
        {
            return;
        }

        var spaBuffer = buffer->Buffer;
        if (_handler is { } handler && spaBuffer->DataCount > 0 && spaBuffer->Datas[0].Data != 0)
        {
            var data = &spaBuffer->Datas[0];
            var offset = Math.Min(data->Chunk->Offset, data->MaxSize);
            var size = Math.Min(data->Chunk->Size, data->MaxSize - offset);
            var samples = (int)(size / sizeof(float));
            samples -= samples % ChannelCount;
            handler.OnData(new ReadOnlySpan<float>((byte*)data->Data + offset, samples));
        }

        StreamQueueBuffer(stream, buffer);
    }
}

/// <summary>
/// Plays to a sink.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed unsafe class PipeWireRenderDevice : PipeWireStreamDevice, IRenderDevice
{
    private volatile IRenderHandler? _handler;

    public PipeWireRenderDevice(PipeWireBackend backend, StreamTarget target)
        : base(backend, target, DirectionOutput, "Playback", [])
    {
        backend.Logger.LogInformation(
            "Playback on {Device}: {Layout}, {Rate} Hz.",
            target.Description,
            target.Layout,
            target.Rate
        );
    }

    public void Start(IRenderHandler handler)
    {
        _handler = handler;
        if (PendingFault is { } fault)
        {
            handler.OnStopped(fault);
        }
    }

    protected override void OnFault(Exception exception)
    {
        _handler?.OnStopped(exception);
    }

    protected override void Process(nint stream)
    {
        var buffer = StreamDequeueBuffer(stream);
        if (buffer == null)
        {
            return;
        }

        var spaBuffer = buffer->Buffer;
        if (spaBuffer->DataCount == 0 || spaBuffer->Datas[0].Data == 0)
        {
            StreamQueueBuffer(stream, buffer);
            return;
        }

        var data = &spaBuffer->Datas[0];
        var stride = ChannelCount * sizeof(float);
        var frames = (int)(data->MaxSize / stride);
        if (buffer->Requested > 0)
        {
            frames = (int)Math.Min((ulong)frames, buffer->Requested);
        }

        var samples = new Span<float>((void*)data->Data, frames * ChannelCount);
        if (_handler is { } handler)
        {
            handler.OnRead(samples);
        }
        else
        {
            samples.Clear();
        }

        data->Chunk->Offset = 0;
        data->Chunk->Stride = stride;
        data->Chunk->Size = (uint)(frames * stride);
        buffer->Size = (ulong)frames;
        StreamQueueBuffer(stream, buffer);
    }
}
