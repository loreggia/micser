using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using static Micser.Audio.Devices.PipeWire.PipeWireNative;

namespace Micser.Audio.Devices.PipeWire;

/// <summary>
/// Audio devices through PipeWire's native API: the sinks and sources of the PipeWire graph, with <c>pw_stream</c>s for capture,
/// loopback (a sink's monitor) and playback. A <c>pw_thread_loop</c> runs PipeWire's events; its lock guards every call into
/// PipeWire. Device state is kept in managed objects under <see cref="_stateLock"/>, which the loop thread takes inside the loop lock,
/// so callers must not take the loop lock while they hold the state lock.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed unsafe class PipeWireBackend : IAudioBackend, PipeWireBackend.ICallbackTarget
{
    /// <summary>
    /// The period streams ask for (<c>node.latency</c>). PipeWire runs a device with the smallest period its streams ask for, so this is
    /// the most a stream gets per cycle; about WASAPI's 10 ms shared-mode period at 48 kHz.
    /// </summary>
    public const int PeriodFrames = 512;

    private const int DefaultGraphRate = 48000;
    private const int RoundtripTimeoutSeconds = 5;

    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(10);

    private static readonly CoreEvents* CoreEventTable = CreateCoreEvents();
    private static readonly Lock InitLock = new();
    private static readonly MetadataEvents* MetadataEventTable = CreateMetadataEvents();
    private static readonly NodeEvents* NodeEventTable = CreateNodeEvents();
    private static readonly RegistryEvents* RegistryEventTable = CreateRegistryEvents();
    private static bool s_isInitialized;

    private readonly List<PipeWireCable> _cables = [];
    private readonly Dictionary<uint, string> _deviceDescriptions = [];
    private readonly List<PipeWireStreamDevice> _endpoints = [];
    private readonly ILogger _logger;
    private readonly Dictionary<uint, Metadata> _metadata = [];
    private readonly Dictionary<uint, Node> _nodes = [];
    private readonly Timer _reconnectTimer;
    private readonly string? _remote;
    private readonly GCHandle _self;
    private readonly Lock _stateLock = new();
    private nint _context;
    private nint _core;
    private readonly SpaHook* _coreHook;
    private string? _defaultSink;
    private string? _defaultSource;
    private volatile bool _isDisconnected;
    private bool _isDisposed;
    private int _forcedRate;
    private int _graphRate = DefaultGraphRate;
    private nint _loop;
    private IReadOnlyList<ChannelLayout> _cableLayouts = [];
    private TimeSpan _reconnectDelay = TimeSpan.FromSeconds(1);
    private nint _registry;
    private readonly SpaHook* _registryHook;
    private volatile bool _syncDone;
    private int _syncSeq;

    private PipeWireBackend(ILogger logger, string? remote)
    {
        _logger = logger;
        _remote = remote;
        _self = GCHandle.Alloc(this);
        _coreHook = AllocHook();
        _registryHook = AllocHook();
        _reconnectTimer = new Timer(_ => Reconnect());
        lock (InitLock)
        {
            if (!s_isInitialized)
            {
                Init(0, 0);
                s_isInitialized = true;
            }
        }

        fixed (byte* name = "micser-pipewire"u8)
        {
            _loop = ThreadLoopNew(name, null);
        }

        if (_loop == 0)
        {
            Dispose();
            throw new InvalidOperationException("Could not create the PipeWire thread loop.");
        }

        _context = ContextNew(ThreadLoopGetLoop(_loop), 0, 0);
        if (_context == 0 || ThreadLoopStart(_loop) < 0)
        {
            Dispose();
            throw new InvalidOperationException("Could not start the PipeWire context.");
        }

        Lock();
        try
        {
            Connect();
        }
        catch
        {
            Unlock();
            Dispose();
            throw;
        }

        Unlock();
        _logger.LogInformation(
            "Connected to PipeWire (library {Version}): {Outputs} outputs, {Inputs} inputs, {Rate} Hz.",
            Utf8ToString((byte*)GetLibraryVersion()),
            GetDevices(DeviceDirection.Output, false).Count,
            GetDevices(DeviceDirection.Input, false).Count,
            GraphRate
        );
    }

    public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;

    /// <summary>
    /// The default output's volume, or which output is the default, changed. Raised on the PipeWire thread.
    /// </summary>
    public event EventHandler? DefaultVolumeChanged;

    public event EventHandler? SystemResumed
    {
        add { }
        remove { }
    }

    /// <summary>
    /// The rate the graph runs at; streams use it, so PipeWire doesn't resample.
    /// </summary>
    public int GraphRate
    {
        get
        {
            lock (_stateLock)
            {
                return _forcedRate > 0 ? _forcedRate : _graphRate;
            }
        }
    }

    PipeWireBackend ICallbackTarget.Backend => this;

    public bool SupportsCables => true;

    internal nint Core => _core;

    internal ILogger Logger => _logger;

    /// <summary>
    /// Connects to the PipeWire daemon of the session, or returns null if libpipewire isn't installed or there's no daemon.
    /// </summary>
    /// <param name="remote">The daemon's socket (a name in the runtime folder or an absolute path), or null for the session's.</param>
    public static PipeWireBackend? TryCreate(ILogger logger, string? remote = null)
    {
        if (!NativeLibrary.TryLoad(Library, out _))
        {
            logger.LogInformation("PipeWire isn't installed ({Library} not found).", Library);
            return null;
        }

        try
        {
            return new PipeWireBackend(logger, remote);
        }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
        {
            logger.LogWarning("PipeWire is unavailable: {Reason}", ex.Message);
            return null;
        }
    }

    public IBackendSystemVolume CreateSystemVolume(AudioDeviceService devices, ILogger logger)
    {
        return new PipeWireSystemVolume(this, logger);
    }

    public void Dispose()
    {
        PipeWireStreamDevice[] endpoints;
        lock (_stateLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            endpoints = [.. _endpoints];
        }

        using (var stopped = new ManualResetEvent(false))
        {
            // waits for a running reconnect, which then sees _isDisposed
            _reconnectTimer.Dispose(stopped);
            stopped.WaitOne();
        }

        foreach (var endpoint in endpoints)
        {
            endpoint.Dispose();
        }

        if (_loop != 0)
        {
            Lock();
            Disconnect();
            foreach (var cable in _cables)
            {
                cable.Dispose();
            }

            _cables.Clear();
            Unlock();
            ThreadLoopStop(_loop);
        }

        if (_context != 0)
        {
            ContextDestroy(_context);
            _context = 0;
        }

        if (_loop != 0)
        {
            ThreadLoopDestroy(_loop);
            _loop = 0;
        }

        NativeMemory.Free(_coreHook);
        NativeMemory.Free(_registryHook);
        _self.Free();
    }

    public AudioDeviceInfo? GetDefaultDevice(DeviceDirection direction)
    {
        lock (_stateLock)
        {
            var name = direction == DeviceDirection.Output ? _defaultSink : _defaultSource;
            var node = FindNode(name);
            return node?.Direction == direction ? Describe(node) : null;
        }
    }

    public AudioDeviceInfo? GetDevice(string id)
    {
        lock (_stateLock)
        {
            var node = FindNode(id);
            return node == null ? null : Describe(node);
        }
    }

    public IReadOnlyList<AudioDeviceInfo> GetDevices(DeviceDirection direction, bool includeInactive)
    {
        // PipeWire only has the nodes of active devices and profiles
        lock (_stateLock)
        {
            return _nodes
                .Values.Where(node => node.IsReady && node.Direction == direction)
                .OrderBy(node => node.Id)
                .Select(Describe)
                .ToList();
        }
    }

    /// <summary>
    /// The default output's volume and mute, or null if there's none or it has no volume yet.
    /// </summary>
    public SystemVolumeLevel? GetDefaultVolume()
    {
        lock (_stateLock)
        {
            var node = FindNode(_defaultSink);
            if (node?.Volumes is not { Length: > 0 } volumes)
            {
                return null;
            }

            // the loudest channel, as pipewire-pulse shows a sink's volume
            return new SystemVolumeLevel(Math.Clamp(volumes.Max(), 0f, 1f), node.Mute ?? false);
        }
    }

    /// <summary>
    /// Creates, changes and removes cables (see <see cref="PipeWireCable"/>), and waits until their devices are listed and the
    /// devices of removed or replaced cables are gone. While the connection is lost, the cables are created once it's back.
    /// </summary>
    public void SetCables(IReadOnlyList<ChannelLayout> layouts)
    {
        Lock();
        try
        {
            _cableLayouts = [.. layouts];
            if (!_isDisconnected && !_isDisposed)
            {
                var removedNodes = new HashSet<uint>();
                ApplyCables(removedNodes);
                WaitForCableDevices(removedNodes);
            }
        }
        finally
        {
            Unlock();
        }
    }

    public ICaptureDevice OpenCapture(string id, bool loopback)
    {
        var target = GetTarget(id, loopback ? DeviceDirection.Output : DeviceDirection.Input);
        return new PipeWireCaptureDevice(this, target, loopback);
    }

    public IRenderDevice OpenRender(string id)
    {
        return new PipeWireRenderDevice(this, GetTarget(id, DeviceDirection.Output));
    }

    /// <summary>
    /// Takes the loop lock; every call into PipeWire outside its callbacks needs it. Recursive.
    /// </summary>
    internal void Lock()
    {
        ThreadLoopLock(_loop);
    }

    /// <summary>
    /// Adds an endpoint, which <see cref="Dispose"/> closes and which faults when its device goes away. False after disposal.
    /// </summary>
    internal bool Register(PipeWireStreamDevice endpoint)
    {
        lock (_stateLock)
        {
            if (_isDisposed || _isDisconnected)
            {
                return false;
            }

            _endpoints.Add(endpoint);
            return true;
        }
    }

    internal void Unlock()
    {
        ThreadLoopUnlock(_loop);
    }

    internal void Unregister(PipeWireStreamDevice endpoint)
    {
        lock (_stateLock)
        {
            _endpoints.Remove(endpoint);
        }
    }

    private static SpaHook* AllocHook()
    {
        return (SpaHook*)NativeMemory.AllocZeroed((nuint)sizeof(SpaHook));
    }

    private static CoreEvents* CreateCoreEvents()
    {
        var events = (CoreEvents*)NativeMemory.AllocZeroed((nuint)sizeof(CoreEvents));
        events->Done = &OnCoreDone;
        events->Error = &OnCoreError;
        return events;
    }

    private static MetadataEvents* CreateMetadataEvents()
    {
        var events = (MetadataEvents*)NativeMemory.AllocZeroed((nuint)sizeof(MetadataEvents));
        events->Property = &OnMetadataProperty;
        return events;
    }

    private static NodeEvents* CreateNodeEvents()
    {
        var events = (NodeEvents*)NativeMemory.AllocZeroed((nuint)sizeof(NodeEvents));
        events->Info = &OnNodeInfo;
        events->Param = &OnNodeParam;
        return events;
    }

    private static RegistryEvents* CreateRegistryEvents()
    {
        var events = (RegistryEvents*)NativeMemory.AllocZeroed((nuint)sizeof(RegistryEvents));
        events->Global = &OnGlobal;
        events->GlobalRemove = &OnGlobalRemove;
        return events;
    }

    /// <summary>
    /// Runs a callback's work for the object behind <paramref name="data"/>; exceptions must not reach PipeWire's thread.
    /// </summary>
    private static void Guard<T>(nint data, Action<T> action)
        where T : class, ICallbackTarget
    {
        if (GCHandle.FromIntPtr(data).Target is not T target)
        {
            return;
        }

        try
        {
            action(target);
        }
        catch (Exception ex)
        {
            target.Backend._logger.LogError(ex, "A PipeWire event handler failed.");
        }
    }

    [UnmanagedCallersOnly]
    private static void OnCoreDone(nint data, uint id, int seq)
    {
        Guard<PipeWireBackend>(
            data,
            backend =>
            {
                if (id == IdCore && seq == backend._syncSeq)
                {
                    backend._syncDone = true;
                    ThreadLoopSignal(backend._loop, false);
                }
            }
        );
    }

    [UnmanagedCallersOnly]
    private static void OnCoreError(nint data, uint id, int seq, int result, byte* message)
    {
        var text = Utf8ToString(message);
        Guard<PipeWireBackend>(data, backend => backend.HandleError(id, result, text));
    }

    [UnmanagedCallersOnly]
    private static void OnGlobal(nint data, uint id, uint permissions, byte* type, uint version, SpaDict* props)
    {
        var typeName = Utf8ToString(type);
        var properties = ReadDict(props);
        Guard<PipeWireBackend>(data, backend => backend.AddGlobal(id, typeName, properties));
    }

    [UnmanagedCallersOnly]
    private static void OnGlobalRemove(nint data, uint id)
    {
        Guard<PipeWireBackend>(data, backend => backend.RemoveGlobal(id));
    }

    [UnmanagedCallersOnly]
    private static int OnMetadataProperty(nint data, uint subject, byte* key, byte* type, byte* value)
    {
        var keyText = Utf8ToString(key);
        var valueText = Utf8ToString(value);
        Guard<Metadata>(data, metadata => metadata.Backend.SetMetadata(metadata.Name, subject, keyText, valueText));
        return 0;
    }

    [UnmanagedCallersOnly]
    private static void OnNodeInfo(nint data, PwNodeInfo* info)
    {
        if ((info->ChangeMask & NodeChangeMaskProps) == 0)
        {
            return;
        }

        var properties = ReadDict(info->Props);
        Guard<Node>(data, node => node.Backend.UpdateNode(node, properties));
    }

    [UnmanagedCallersOnly]
    private static void OnNodeParam(nint data, int seq, uint id, uint index, uint next, nint param)
    {
        if (id != ParamProps || param == 0)
        {
            return;
        }

        var pod = new ReadOnlySpan<byte>((void*)param, SpaPod.GetSize(new ReadOnlySpan<byte>((void*)param, 8)));
        var (volumes, mute) = SpaPod.ReadVolume(pod);
        Guard<Node>(data, node => node.Backend.UpdateVolume(node, volumes, mute));
    }

    private static string? ParseDefaultName(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("name", out var name) ? name.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void AddGlobal(uint id, string? type, Dictionary<string, string> properties)
    {
        switch (type)
        {
            case TypeNode:
                AddNode(id, properties);
                break;

            case TypeDevice when properties.TryGetValue("device.description", out var description):
                lock (_stateLock)
                {
                    _deviceDescriptions[id] = description;
                }

                break;

            case TypeMetadata
                when properties.GetValueOrDefault("metadata.name") is { } name && name is "default" or "settings":
                var metadata = new Metadata(this, name);
                fixed (byte* typeName = "PipeWire:Interface:Metadata"u8)
                {
                    metadata.Proxy = RegistryBind(_registry, id, typeName, VersionMetadata);
                }

                if (metadata.Proxy != 0)
                {
                    MetadataAddListener(
                        metadata.Proxy,
                        metadata.Hook,
                        (nint)MetadataEventTable,
                        metadata.HandlePointer
                    );
                    lock (_stateLock)
                    {
                        _metadata[id] = metadata;
                    }
                }
                else
                {
                    metadata.Destroy();
                }

                break;
        }
    }

    private void AddNode(uint id, Dictionary<string, string> properties)
    {
        DeviceDirection direction;
        switch (properties.GetValueOrDefault("media.class"))
        {
            case "Audio/Sink":
                direction = DeviceDirection.Output;
                break;
            case "Audio/Source":
            case "Audio/Source/Virtual":
                direction = DeviceDirection.Input;
                break;
            default:
                return;
        }

        var node = new Node(this, id, direction);
        node.Update(properties);
        fixed (byte* typeName = "PipeWire:Interface:Node"u8)
        {
            node.Proxy = RegistryBind(_registry, id, typeName, VersionNode);
        }

        if (node.Proxy == 0)
        {
            node.Destroy();
            return;
        }

        NodeAddListener(node.Proxy, node.Hook, (nint)NodeEventTable, node.HandlePointer);
        if (direction == DeviceDirection.Output)
        {
            var props = ParamProps;
            NodeSubscribeParams(node.Proxy, &props, 1);
        }

        lock (_stateLock)
        {
            _nodes[id] = node;
        }
    }

    /// <summary>
    /// Connects to the daemon and waits until the devices and metadata are known. Loop lock held, not on the loop thread.
    /// </summary>
    /// <exception cref="InvalidOperationException">There's no daemon.</exception>
    /// <exception cref="TimeoutException">The daemon didn't answer.</exception>
    private void Connect()
    {
        List<KeyValuePair<string, string>> properties = [new("application.name", "Micser")];
        if (_remote != null)
        {
            properties.Add(new("remote.name", _remote));
        }

        _core = ContextConnect(_context, NewProperties(properties), 0);
        if (_core == 0)
        {
            throw new InvalidOperationException("Could not connect to PipeWire; the daemon may not be running.");
        }

        _isDisconnected = false;
        CoreAddListener(_core, _coreHook, (nint)CoreEventTable, GCHandle.ToIntPtr(_self));
        _registry = CoreGetRegistry(_core, VersionRegistry);
        RegistryAddListener(_registry, _registryHook, (nint)RegistryEventTable, GCHandle.ToIntPtr(_self));

        // the first roundtrip announces the globals, the second delivers the bound nodes' info and the metadata
        Roundtrip();
        Roundtrip();

        if (_cableLayouts.Count > 0)
        {
            ApplyCables();
            Roundtrip();
            Roundtrip();
        }
    }

    /// <summary>
    /// Makes the loaded cables match <see cref="_cableLayouts"/>: removes those beyond the count or with another layout, and loads
    /// missing ones. Loop lock held.
    /// </summary>
    /// <param name="removedNodes">Gets the ids of the removed cables' nodes, which the daemon removes later.</param>
    private void ApplyCables(HashSet<uint>? removedNodes = null)
    {
        for (var i = _cables.Count - 1; i >= 0; i--)
        {
            if (i >= _cableLayouts.Count || _cables[i].Layout != _cableLayouts[i])
            {
                if (removedNodes != null)
                {
                    var names = new[]
                    {
                        PipeWireCable.InputName(_cables[i].Number),
                        PipeWireCable.OutputName(_cables[i].Number),
                    };
                    lock (_stateLock)
                    {
                        removedNodes.UnionWith(
                            _nodes.Where(pair => names.Contains(pair.Value.Name)).Select(pair => pair.Key)
                        );
                    }
                }

                _cables[i].Dispose();
                _cables.RemoveAt(i);
            }
        }

        for (var i = 0; i < _cableLayouts.Count; i++)
        {
            if (i == _cables.Count || _cables[i].Number != i + 1)
            {
                _cables.Insert(i, new PipeWireCable(i + 1, _cableLayouts[i], _logger));
            }

            _cables[i].Load(_context, _remote);
        }
    }

    private AudioDeviceInfo Describe(Node node)
    {
        var (layout, _) = SpaAudioChannels.Negotiate(node.Positions, node.ChannelCount);
        var adapter =
            node.DeviceId is { } deviceId && _deviceDescriptions.TryGetValue(deviceId, out var description)
                ? description
                : node.Nick;
        return new AudioDeviceInfo(
            node.Name!,
            node.Description ?? node.Name!,
            node.ProfileDescription ?? node.Nick,
            adapter,
            node.Direction,
            true,
            layout,
            _forcedRate > 0 ? _forcedRate : _graphRate
        );
    }

    /// <summary>
    /// Destroys the streams, proxies and core of the connection. Loop lock held.
    /// </summary>
    private void Disconnect()
    {
        PipeWireStreamDevice[] endpoints;
        lock (_stateLock)
        {
            endpoints = [.. _endpoints];
        }

        // the streams and cables belong to the core, so they go first; the streams' devices dispose them later
        foreach (var endpoint in endpoints)
        {
            endpoint.DestroyStream();
        }

        foreach (var cable in _cables)
        {
            cable.Unload();
        }

        lock (_stateLock)
        {
            foreach (var node in _nodes.Values)
            {
                node.Destroy();
            }

            foreach (var metadata in _metadata.Values)
            {
                metadata.Destroy();
            }

            _nodes.Clear();
            _metadata.Clear();
            _deviceDescriptions.Clear();
            _defaultSink = null;
            _defaultSource = null;
        }

        if (_registry != 0)
        {
            HookRemove(_registryHook);
            ProxyDestroy(_registry);
            _registry = 0;
        }

        if (_core != 0)
        {
            HookRemove(_coreHook);
            CoreDisconnect(_core);
            _core = 0;
        }
    }

    private void FaultEndpoints(uint? nodeId, string reason)
    {
        PipeWireStreamDevice[] endpoints;
        lock (_stateLock)
        {
            endpoints = _endpoints.Where(endpoint => nodeId == null || endpoint.Target.NodeId == nodeId).ToArray();
        }

        foreach (var endpoint in endpoints)
        {
            endpoint.Fault(reason);
        }
    }

    /// <summary>
    /// A ready node by name; state lock held.
    /// </summary>
    private Node? FindNode(string? name)
    {
        return name == null ? null : _nodes.Values.FirstOrDefault(node => node.IsReady && node.Name == name);
    }

    private StreamTarget GetTarget(string id, DeviceDirection direction)
    {
        lock (_stateLock)
        {
            var node = FindNode(id) ?? throw new InvalidOperationException($"Audio device '{id}' not found.");
            if (node.Direction != direction)
            {
                throw new InvalidOperationException(
                    $"Audio device '{id}' is not an active {(direction == DeviceDirection.Input ? "input" : "output")}."
                );
            }

            var (layout, positions) = SpaAudioChannels.Negotiate(node.Positions, node.ChannelCount);
            return new StreamTarget(
                node.Id,
                node.Name!,
                node.Description ?? node.Name!,
                layout,
                positions,
                _forcedRate > 0 ? _forcedRate : _graphRate
            );
        }
    }

    private void HandleError(uint id, int result, string? message)
    {
        if (id != IdCore || result != -ErrorBrokenPipe)
        {
            _logger.LogWarning("PipeWire error on object {Id}: {Message} ({Result}).", id, message, result);
            return;
        }

        _logger.LogError("Lost the connection to PipeWire: {Message}.", message);
        _isDisconnected = true;
        FaultEndpoints(null, "the connection to PipeWire was lost");
        List<Node> removed;
        lock (_stateLock)
        {
            removed = _nodes.Values.Where(node => node.IsReady).ToList();
            foreach (var node in _nodes.Values)
            {
                node.IsReady = false;
            }
        }

        foreach (var node in removed)
        {
            Raise(node.Name!, AudioDeviceChange.Removed);
        }

        ThreadLoopSignal(_loop, false);

        // the core can't be destroyed inside its own event
        lock (_stateLock)
        {
            if (!_isDisposed)
            {
                _reconnectTimer.Change(_reconnectDelay, Timeout.InfiniteTimeSpan);
            }
        }
    }

    /// <summary>
    /// Waits until the devices of the loaded cables are listed and <paramref name="removedNodes"/> are gone, at most 3 s: the loopback
    /// module connects its streams one after the other, and its own connection closes after it was destroyed, so a roundtrip isn't
    /// enough. Loop lock held, not on the loop thread.
    /// </summary>
    private void WaitForCableDevices(HashSet<uint> removedNodes)
    {
        Roundtrip();
        var names = _cables
            .Where(cable => cable.IsLoaded)
            .SelectMany(cable =>
                new[] { PipeWireCable.InputName(cable.Number), PipeWireCable.OutputName(cable.Number) }
            )
            .ToArray();
        var deadline = Environment.TickCount64 + 3000;
        while (!_isDisconnected && Environment.TickCount64 < deadline)
        {
            lock (_stateLock)
            {
                // a replaced cable's old nodes have the names of the new ones
                if (!removedNodes.Any(_nodes.ContainsKey) && names.All(name => FindNode(name) != null))
                {
                    return;
                }
            }

            ThreadLoopTimedWait(_loop, 1);
        }

        _logger.LogWarning("The virtual cables' devices didn't all appear.");
    }

    private void Raise(string deviceId, AudioDeviceChange change)
    {
        DeviceChanged?.Invoke(this, new AudioDeviceChangedEventArgs(deviceId, change));
    }

    /// <summary>
    /// Replaces a lost connection with a new one, retrying with a growing delay while there's no daemon. Timer thread.
    /// </summary>
    private void Reconnect()
    {
        Lock();
        try
        {
            if (_isDisposed || !_isDisconnected)
            {
                return;
            }

            Disconnect();
            Connect();
            _reconnectDelay = TimeSpan.FromSeconds(1);
            _logger.LogInformation(
                "Reconnected to PipeWire: {Outputs} outputs, {Inputs} inputs.",
                GetDevices(DeviceDirection.Output, false).Count,
                GetDevices(DeviceDirection.Input, false).Count
            );
        }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
        {
            _isDisconnected = true;
            _logger.LogDebug("Reconnecting to PipeWire failed: {Reason}", ex.Message);
            try
            {
                _reconnectTimer.Change(_reconnectDelay, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // the backend is being disposed
            }

            _reconnectDelay = TimeSpan.FromTicks(Math.Min(_reconnectDelay.Ticks * 2, MaxReconnectDelay.Ticks));
        }
        finally
        {
            Unlock();
        }
    }

    private void RemoveGlobal(uint id)
    {
        Node? node;
        Metadata? metadata;
        lock (_stateLock)
        {
            _deviceDescriptions.Remove(id);
            _nodes.Remove(id, out node);
            _metadata.Remove(id, out metadata);
        }

        metadata?.Destroy();
        if (node == null)
        {
            return;
        }

        var wasReady = node.IsReady;
        node.Destroy();
        FaultEndpoints(id, "the device was removed");
        if (wasReady)
        {
            Raise(node.Name!, AudioDeviceChange.Removed);
        }

        // for WaitForCableDevices
        ThreadLoopSignal(_loop, false);
    }

    /// <summary>
    /// Waits until the daemon processed everything sent so far, and its answers were handled. Loop lock held, not on the loop thread.
    /// </summary>
    private void Roundtrip()
    {
        _syncDone = false;
        _syncSeq = CoreSync(_core, IdCore, 0);
        while (!_syncDone && !_isDisconnected)
        {
            if (ThreadLoopTimedWait(_loop, RoundtripTimeoutSeconds) != 0)
            {
                throw new TimeoutException("PipeWire didn't answer.");
            }
        }
    }

    /// <summary>
    /// A property of the <c>default</c> or <c>settings</c> metadata changed; a null key removed all properties of the subject. Only
    /// subject 0 (the core) has the defaults and settings: the others are objects such as streams, whose properties (e.g. their target)
    /// are removed when they go away.
    /// </summary>
    private void SetMetadata(string name, uint subject, string? key, string? value)
    {
        if (subject != IdCore)
        {
            return;
        }

        if (name == "settings")
        {
            if (key is "clock.rate" or "clock.force-rate" && int.TryParse(value, out var rate))
            {
                lock (_stateLock)
                {
                    if (key == "clock.rate")
                    {
                        _graphRate = rate > 0 ? rate : DefaultGraphRate;
                    }
                    else
                    {
                        _forcedRate = rate;
                    }
                }
            }

            return;
        }

        var sinkChanged = false;
        var sourceChanged = false;
        string? sink;
        string? source;
        lock (_stateLock)
        {
            if (key is null or "default.audio.sink")
            {
                var newSink = key == null ? null : ParseDefaultName(value);
                sinkChanged = newSink != _defaultSink;
                _defaultSink = newSink;
            }

            if (key is null or "default.audio.source")
            {
                var newSource = key == null ? null : ParseDefaultName(value);
                sourceChanged = newSource != _defaultSource;
                _defaultSource = newSource;
            }

            sink = _defaultSink;
            source = _defaultSource;
        }

        if (sinkChanged || sourceChanged)
        {
            _logger.LogDebug("Default output {Sink}, input {Source}.", sink ?? "-", source ?? "-");
        }

        if (sinkChanged)
        {
            Raise(sink ?? "", AudioDeviceChange.DefaultChanged);
            DefaultVolumeChanged?.Invoke(this, EventArgs.Empty);
        }

        if (sourceChanged)
        {
            Raise(source ?? "", AudioDeviceChange.DefaultChanged);
        }
    }

    private void UpdateNode(Node node, Dictionary<string, string> properties)
    {
        bool wasReady;
        lock (_stateLock)
        {
            if (!_nodes.ContainsKey(node.Id))
            {
                return;
            }

            node.Update(properties);
            wasReady = node.IsReady;
            node.IsReady = node.Name != null;
        }

        if (node.IsReady)
        {
            Raise(node.Name!, wasReady ? AudioDeviceChange.StateChanged : AudioDeviceChange.Added);

            // for WaitForCableDevices
            ThreadLoopSignal(_loop, false);
        }
    }

    private void UpdateVolume(Node node, float[]? volumes, bool? mute)
    {
        bool isDefault;
        lock (_stateLock)
        {
            node.Volumes = volumes ?? node.Volumes;
            node.Mute = mute ?? node.Mute;
            isDefault = node.Name == _defaultSink;
        }

        if (volumes != null || mute != null)
        {
            _logger.LogDebug(
                "PipeWire volume of {Device}{Default}: {Volumes}, mute {Mute}.",
                node.Name,
                isDefault ? " (default)" : "",
                volumes == null ? "-" : string.Join(", ", volumes.Select(v => v.ToString("0.#####"))),
                mute?.ToString() ?? "-"
            );
        }

        if (isDefault)
        {
            DefaultVolumeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// An object whose callbacks get a <see cref="GCHandle"/> to it as their data.
    /// </summary>
    private interface ICallbackTarget
    {
        PipeWireBackend Backend { get; }
    }

    /// <summary>
    /// A proxy for a global with one listener.
    /// </summary>
    private abstract class BoundObject : ICallbackTarget
    {
        private GCHandle _handle;

        protected BoundObject(PipeWireBackend backend)
        {
            Backend = backend;
            Hook = AllocHook();
            _handle = GCHandle.Alloc(this);
        }

        public PipeWireBackend Backend { get; }

        public nint HandlePointer => GCHandle.ToIntPtr(_handle);

        public SpaHook* Hook { get; }

        public nint Proxy { get; set; }

        /// <summary>
        /// Loop lock held.
        /// </summary>
        public void Destroy()
        {
            if (!_handle.IsAllocated)
            {
                return;
            }

            HookRemove(Hook);
            if (Proxy != 0)
            {
                ProxyDestroy(Proxy);
                Proxy = 0;
            }

            NativeMemory.Free(Hook);
            _handle.Free();
        }
    }

    /// <summary>
    /// The <c>default</c> (default devices) or <c>settings</c> (graph rate) metadata.
    /// </summary>
    private sealed class Metadata : BoundObject
    {
        public Metadata(PipeWireBackend backend, string name)
            : base(backend)
        {
            Name = name;
        }

        public string Name { get; }
    }

    /// <summary>
    /// A sink or source. Its properties come from the registry and then from the bound node's info, which also has the channels.
    /// </summary>
    private sealed class Node : BoundObject
    {
        public Node(PipeWireBackend backend, uint id, DeviceDirection direction)
            : base(backend)
        {
            Id = id;
            Direction = direction;
        }

        public int ChannelCount { get; private set; }

        public string? Description { get; private set; }

        public uint? DeviceId { get; private set; }

        public DeviceDirection Direction { get; }

        /// <summary>
        /// Set once the node's info arrived, so its channels are known.
        /// </summary>
        public bool IsReady { get; set; }

        public bool? Mute { get; set; }

        public string? Nick { get; private set; }

        public uint Id { get; }

        public string? Name { get; private set; }

        public IReadOnlyList<string> Positions { get; private set; } = [];

        public string? ProfileDescription { get; private set; }

        public float[]? Volumes { get; set; }

        public void Update(Dictionary<string, string> properties)
        {
            Name = properties.GetValueOrDefault("node.name") ?? Name;
            Description = properties.GetValueOrDefault("node.description") ?? Description;
            Nick = properties.GetValueOrDefault("node.nick") ?? Nick;
            ProfileDescription = properties.GetValueOrDefault("device.profile.description") ?? ProfileDescription;
            if (uint.TryParse(properties.GetValueOrDefault("device.id"), out var deviceId))
            {
                DeviceId = deviceId;
            }

            if (int.TryParse(properties.GetValueOrDefault("audio.channels"), out var channels))
            {
                ChannelCount = channels;
            }

            if (properties.TryGetValue("audio.position", out var positions))
            {
                Positions = SpaAudioChannels.ParsePositions(positions);
            }
        }
    }
}

/// <summary>
/// The node a stream connects to, and the format it uses.
/// </summary>
/// <param name="Positions">The channel positions to ask for, in <paramref name="Layout"/>'s order.</param>
/// <param name="Rate">The graph's rate.</param>
internal sealed record StreamTarget(
    uint NodeId,
    string Name,
    string Description,
    ChannelLayout Layout,
    uint[] Positions,
    int Rate
);
