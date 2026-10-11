using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Micser.Audio.Devices.PipeWire;

/// <summary>
/// The parts of libpipewire-0.3 and SPA that the backend uses. Values and layouts are those of PipeWire 1.0 (stable ABI since 0.3) on
/// 64-bit Linux. Interface methods that the C API only has as inline functions (core, registry, node, metadata) are called through
/// the objects' <c>spa_interface</c> method tables (<see cref="SpaInterface"/>).
/// </summary>
[SupportedOSPlatform("linux")]
internal static unsafe partial class PipeWireNative
{
    public const string Library = "libpipewire-0.3.so.0";

    public const uint IdAny = 0xFFFFFFFF;
    public const uint IdCore = 0;

    public const string TypeDevice = "PipeWire:Interface:Device";
    public const string TypeMetadata = "PipeWire:Interface:Metadata";
    public const string TypeNode = "PipeWire:Interface:Node";

    public const uint VersionMetadata = 3;
    public const uint VersionNode = 3;
    public const uint VersionRegistry = 3;

    public const int DirectionInput = 0;
    public const int DirectionOutput = 1;

    public const uint StreamFlagAutoconnect = 1 << 0;
    public const uint StreamFlagMapBuffers = 1 << 2;
    public const uint StreamFlagRtProcess = 1 << 4;
    public const uint StreamFlagDontReconnect = 1 << 7;

    public const int StreamStateError = -1;
    public const int StreamStateUnconnected = 0;
    public const int StreamStateStreaming = 3;

    public const ulong NodeChangeMaskProps = 1 << 3;

    public const uint ParamProps = 2;
    public const uint ParamEnumFormat = 3;

    public const int ErrorBrokenPipe = 32;

    [LibraryImport(Library, EntryPoint = "pw_context_connect")]
    public static partial nint ContextConnect(nint context, nint properties, nuint userDataSize);

    [LibraryImport(Library, EntryPoint = "pw_context_destroy")]
    public static partial void ContextDestroy(nint context);

    [LibraryImport(Library, EntryPoint = "pw_context_new")]
    public static partial nint ContextNew(nint mainLoop, nint properties, nuint userDataSize);

    [LibraryImport(Library, EntryPoint = "pw_core_disconnect")]
    public static partial int CoreDisconnect(nint core);

    [LibraryImport(Library, EntryPoint = "pw_get_library_version")]
    public static partial nint GetLibraryVersion();

    [LibraryImport(Library, EntryPoint = "pw_init")]
    public static partial void Init(nint argc, nint argv);

    [LibraryImport(Library, EntryPoint = "pw_properties_new_dict")]
    public static partial nint PropertiesNewDict(SpaDict* dict);

    [LibraryImport(Library, EntryPoint = "pw_proxy_destroy")]
    public static partial void ProxyDestroy(nint proxy);

    [LibraryImport(Library, EntryPoint = "pw_stream_add_listener")]
    public static partial void StreamAddListener(nint stream, SpaHook* listener, nint events, nint data);

    [LibraryImport(Library, EntryPoint = "pw_stream_connect")]
    public static partial int StreamConnect(
        nint stream,
        int direction,
        uint targetId,
        uint flags,
        nint* parameters,
        uint parameterCount
    );

    [LibraryImport(Library, EntryPoint = "pw_stream_dequeue_buffer")]
    public static partial PwBuffer* StreamDequeueBuffer(nint stream);

    [LibraryImport(Library, EntryPoint = "pw_stream_destroy")]
    public static partial void StreamDestroy(nint stream);

    [LibraryImport(Library, EntryPoint = "pw_stream_new")]
    public static partial nint StreamNew(nint core, byte* name, nint properties);

    [LibraryImport(Library, EntryPoint = "pw_stream_queue_buffer")]
    public static partial int StreamQueueBuffer(nint stream, PwBuffer* buffer);

    [LibraryImport(Library, EntryPoint = "pw_thread_loop_destroy")]
    public static partial void ThreadLoopDestroy(nint loop);

    [LibraryImport(Library, EntryPoint = "pw_thread_loop_get_loop")]
    public static partial nint ThreadLoopGetLoop(nint loop);

    [LibraryImport(Library, EntryPoint = "pw_thread_loop_lock")]
    public static partial void ThreadLoopLock(nint loop);

    [LibraryImport(Library, EntryPoint = "pw_thread_loop_new")]
    public static partial nint ThreadLoopNew(byte* name, SpaDict* properties);

    [LibraryImport(Library, EntryPoint = "pw_thread_loop_signal")]
    public static partial void ThreadLoopSignal(nint loop, [MarshalAs(UnmanagedType.U1)] bool waitForAccept);

    [LibraryImport(Library, EntryPoint = "pw_thread_loop_start")]
    public static partial int ThreadLoopStart(nint loop);

    [LibraryImport(Library, EntryPoint = "pw_thread_loop_stop")]
    public static partial void ThreadLoopStop(nint loop);

    /// <summary>
    /// Waits for <see cref="ThreadLoopSignal"/>; returns 0, or non-zero when the time ran out.
    /// </summary>
    [LibraryImport(Library, EntryPoint = "pw_thread_loop_timed_wait")]
    public static partial int ThreadLoopTimedWait(nint loop, int waitMaxSeconds);

    [LibraryImport(Library, EntryPoint = "pw_thread_loop_unlock")]
    public static partial void ThreadLoopUnlock(nint loop);

    /// <summary>
    /// <c>pw_core_add_listener</c>.
    /// </summary>
    public static int CoreAddListener(nint core, SpaHook* listener, nint events, nint data)
    {
        return ((delegate* unmanaged<nint, SpaHook*, nint, nint, int>)Method(core, 1))(
            Data(core),
            listener,
            events,
            data
        );
    }

    /// <summary>
    /// <c>pw_core_get_registry</c>.
    /// </summary>
    public static nint CoreGetRegistry(nint core, uint version)
    {
        return ((delegate* unmanaged<nint, uint, nuint, nint>)Method(core, 6))(Data(core), version, 0);
    }

    /// <summary>
    /// <c>pw_core_sync</c>: the core answers with a <c>done</c> event carrying the returned sequence number.
    /// </summary>
    public static int CoreSync(nint core, uint id, int seq)
    {
        return ((delegate* unmanaged<nint, uint, int, int>)Method(core, 3))(Data(core), id, seq);
    }

    /// <summary>
    /// <c>pw_metadata_add_listener</c>.
    /// </summary>
    public static int MetadataAddListener(nint metadata, SpaHook* listener, nint events, nint data)
    {
        return ((delegate* unmanaged<nint, SpaHook*, nint, nint, int>)Method(metadata, 1))(
            Data(metadata),
            listener,
            events,
            data
        );
    }

    /// <summary>
    /// <c>pw_node_add_listener</c>.
    /// </summary>
    public static int NodeAddListener(nint node, SpaHook* listener, nint events, nint data)
    {
        return ((delegate* unmanaged<nint, SpaHook*, nint, nint, int>)Method(node, 1))(
            Data(node),
            listener,
            events,
            data
        );
    }

    /// <summary>
    /// <c>pw_node_subscribe_params</c>: the node sends a <c>param</c> event for each of them now and whenever they change.
    /// </summary>
    public static int NodeSubscribeParams(nint node, uint* ids, uint count)
    {
        return ((delegate* unmanaged<nint, uint*, uint, int>)Method(node, 2))(Data(node), ids, count);
    }

    /// <summary>
    /// <c>pw_registry_add_listener</c>.
    /// </summary>
    public static int RegistryAddListener(nint registry, SpaHook* listener, nint events, nint data)
    {
        return ((delegate* unmanaged<nint, SpaHook*, nint, nint, int>)Method(registry, 1))(
            Data(registry),
            listener,
            events,
            data
        );
    }

    /// <summary>
    /// <c>pw_registry_bind</c>: a proxy for the global, or 0.
    /// </summary>
    public static nint RegistryBind(nint registry, uint id, byte* type, uint version)
    {
        return ((delegate* unmanaged<nint, uint, byte*, uint, nuint, nint>)Method(registry, 2))(
            Data(registry),
            id,
            type,
            version,
            0
        );
    }

    /// <summary>
    /// <c>spa_hook_remove</c>: unlinks a listener from the hook list it was added to.
    /// </summary>
    public static void HookRemove(SpaHook* hook)
    {
        if (hook->Next == null)
        {
            return;
        }

        hook->Next->Prev = hook->Prev;
        hook->Prev->Next = hook->Next;
        hook->Next = null;
        hook->Prev = null;
        if (hook->Removed != 0)
        {
            ((delegate* unmanaged<SpaHook*, void>)hook->Removed)(hook);
        }
    }

    /// <summary>
    /// <c>pw_properties</c> with the items; the caller passes ownership on (e.g. to <see cref="StreamNew"/>).
    /// </summary>
    public static nint NewProperties(IReadOnlyList<KeyValuePair<string, string>> items)
    {
        var strings = new List<nint>(items.Count * 2);
        var dictItems = (SpaDictItem*)NativeMemory.Alloc((nuint)Math.Max(items.Count, 1), (nuint)sizeof(SpaDictItem));
        try
        {
            for (var i = 0; i < items.Count; i++)
            {
                var key = Marshal.StringToCoTaskMemUTF8(items[i].Key);
                strings.Add(key);
                var value = Marshal.StringToCoTaskMemUTF8(items[i].Value);
                strings.Add(value);
                dictItems[i] = new SpaDictItem { Key = (byte*)key, Value = (byte*)value };
            }

            var dict = new SpaDict { Count = (uint)items.Count, Items = dictItems };
            return PropertiesNewDict(&dict);
        }
        finally
        {
            foreach (var s in strings)
            {
                Marshal.FreeCoTaskMem(s);
            }

            NativeMemory.Free(dictItems);
        }
    }

    /// <summary>
    /// The items of a <c>spa_dict</c>; later items win over earlier ones with the same key.
    /// </summary>
    public static Dictionary<string, string> ReadDict(SpaDict* dict)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (dict == null)
        {
            return result;
        }

        for (var i = 0; i < dict->Count; i++)
        {
            var key = Utf8ToString(dict->Items[i].Key);
            if (key != null)
            {
                result[key] = Utf8ToString(dict->Items[i].Value) ?? "";
            }
        }

        return result;
    }

    public static string? Utf8ToString(byte* utf8)
    {
        return utf8 == null ? null : Marshal.PtrToStringUTF8((nint)utf8);
    }

    private static nint Data(nint obj)
    {
        return ((SpaInterface*)obj)->Data;
    }

    /// <summary>
    /// The method at <paramref name="index"/> of the object's method table (after the <c>uint32_t version</c>, which is padded to 8 bytes).
    /// </summary>
    private static nint Method(nint obj, int index)
    {
        var method = ((nint*)((SpaInterface*)obj)->Funcs)[index];
        return method != 0
            ? method
            : throw new InvalidOperationException($"PipeWire object {obj} has no method {index}.");
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaInterface
    {
        public nint Type;
        public uint Version;
        public nint Funcs;
        public nint Data;
    }

    /// <summary>
    /// <c>struct spa_hook</c>, zero-initialized before it's added.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SpaHook
    {
        public SpaHook* Next;
        public SpaHook* Prev;
        public nint Funcs;
        public nint Data;
        public nint Removed;
        public nint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaDict
    {
        public uint Flags;
        public uint Count;
        public SpaDictItem* Items;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaDictItem
    {
        public byte* Key;
        public byte* Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PwBuffer
    {
        public SpaBuffer* Buffer;
        public nint UserData;
        public ulong Size;

        /// <summary>
        /// For playback streams the frames the stream wants, or 0 for no suggestion.
        /// </summary>
        public ulong Requested;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaBuffer
    {
        public uint MetaCount;
        public uint DataCount;
        public nint Metas;
        public SpaData* Datas;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaData
    {
        public uint Type;
        public uint Flags;
        public long Fd;
        public uint MapOffset;
        public uint MaxSize;
        public nint Data;
        public SpaChunk* Chunk;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaChunk
    {
        public uint Offset;
        public uint Size;
        public int Stride;
        public int Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PwNodeInfo
    {
        public uint Id;
        public uint MaxInputPorts;
        public uint MaxOutputPorts;
        public ulong ChangeMask;
        public uint InputPortCount;
        public uint OutputPortCount;
        public int State;
        public nint Error;
        public SpaDict* Props;
        public nint Params;
        public uint ParamCount;
    }

    /// <summary>
    /// <c>struct pw_stream_events</c> (version 2).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StreamEvents
    {
        public uint Version;
        public nint Destroy;
        public delegate* unmanaged<nint, int, int, byte*, void> StateChanged;
        public nint ControlInfo;
        public nint IoChanged;
        public delegate* unmanaged<nint, uint, nint, void> ParamChanged;
        public nint AddBuffer;
        public nint RemoveBuffer;
        public delegate* unmanaged<nint, void> Process;
        public nint Drained;
        public nint Command;
        public nint TriggerDone;
    }

    /// <summary>
    /// <c>struct pw_core_events</c> (version 0: up to <c>remove_mem</c>).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CoreEvents
    {
        public uint Version;
        public nint Info;
        public delegate* unmanaged<nint, uint, int, void> Done;
        public nint Ping;
        public delegate* unmanaged<nint, uint, int, int, byte*, void> Error;
        public nint RemoveId;
        public nint BoundId;
        public nint AddMem;
        public nint RemoveMem;
        public nint BoundProps;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RegistryEvents
    {
        public uint Version;
        public delegate* unmanaged<nint, uint, uint, byte*, uint, SpaDict*, void> Global;
        public delegate* unmanaged<nint, uint, void> GlobalRemove;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NodeEvents
    {
        public uint Version;
        public delegate* unmanaged<nint, PwNodeInfo*, void> Info;
        public delegate* unmanaged<nint, int, uint, uint, uint, nint, void> Param;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MetadataEvents
    {
        public uint Version;
        public delegate* unmanaged<nint, uint, byte*, byte*, byte*, int> Property;
    }
}
