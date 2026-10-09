using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Micser.DriverUtility;

/// <summary>
/// The Core Audio COM interfaces DriverUtility uses, source-generated for Native AOT. Interfaces only declare their methods up to the last
/// one used.
/// </summary>
internal static partial class CoreAudio
{
    public const int DataFlowAll = 2;
    public const uint DeviceStateActive = 0x1;
    public const uint StorageRead = 0;

    private const uint ClsctxInprocServer = 0x1;
    private const uint CoinitMultithreaded = 0x0;

    private static readonly StrategyBasedComWrappers ComWrappers = new();

    public static IMMDeviceEnumerator CreateDeviceEnumerator()
    {
        return Create<IMMDeviceEnumerator>(
            new Guid("bcde0395-e52f-467c-8e3d-c4579291692e"),
            typeof(IMMDeviceEnumerator).GUID
        );
    }

    /// <summary>
    /// The undocumented policy object behind the Advanced tab of the Sound control panel; the only way to change an endpoint's device format.
    /// </summary>
    public static IPolicyConfig CreatePolicyConfig()
    {
        return Create<IPolicyConfig>(new Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9"), typeof(IPolicyConfig).GUID);
    }

    [LibraryImport("ole32.dll")]
    public static partial void CoTaskMemFree(nint pointer);

    [LibraryImport("ole32.dll")]
    public static partial int PropVariantClear(ref PROPVARIANT value);

    private static T Create<T>(Guid classId, Guid interfaceId)
    {
        // S_FALSE or RPC_E_CHANGED_MODE when the thread already has COM, which is fine either way
        CoInitializeEx(0, CoinitMultithreaded);
        Marshal.ThrowExceptionForHR(CoCreateInstance(classId, 0, ClsctxInprocServer, interfaceId, out var pointer));
        try
        {
            return (T)ComWrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.None);
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(
        in Guid classId,
        nint outer,
        uint context,
        in Guid interfaceId,
        out nint instance
    );

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(nint reserved, uint coInit);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct PROPERTYKEY
{
    public readonly Guid FmtId;
    public readonly uint Pid;

    public PROPERTYKEY(Guid fmtId, uint pid)
    {
        FmtId = fmtId;
        Pid = pid;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct PROPVARIANT
{
    public const ushort VtLpwstr = 31;

    public ushort VarType;
    public ushort Reserved1;
    public ushort Reserved2;
    public ushort Reserved3;
    public nint Value;
    public nint Value2;
}

[GeneratedComInterface]
[Guid("a95664d2-9614-4f35-a746-de8db63617e6")]
internal partial interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(int dataFlow, uint stateMask, out IMMDeviceCollection devices);
}

[GeneratedComInterface]
[Guid("0bd7a1be-7a1a-44db-8397-cc5392387b5e")]
internal partial interface IMMDeviceCollection
{
    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int Item(uint index, out IMMDevice device);
}

[GeneratedComInterface]
[Guid("d666063f-1587-4e43-81f1-b948e807363f")]
internal partial interface IMMDevice
{
    [PreserveSig]
    int Activate(in Guid interfaceId, uint context, nint parameters, out nint instance);

    [PreserveSig]
    int OpenPropertyStore(uint access, out IPropertyStore properties);

    /// <param name="id">A string to free with <see cref="CoreAudio.CoTaskMemFree"/>.</param>
    [PreserveSig]
    int GetId(out nint id);
}

[GeneratedComInterface]
[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
internal partial interface IPropertyStore
{
    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int GetAt(uint index, out PROPERTYKEY key);

    [PreserveSig]
    int GetValue(in PROPERTYKEY key, out PROPVARIANT value);
}

/// <summary>
/// The formats are WAVEFORMATEX(TENSIBLE) pointers; returned ones are freed with <see cref="CoreAudio.CoTaskMemFree"/>.
/// </summary>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
internal partial interface IPolicyConfig
{
    [PreserveSig]
    int GetMixFormat(string deviceId, out nint format);

    [PreserveSig]
    int GetDeviceFormat(string deviceId, int isDefault, out nint format);

    [PreserveSig]
    int ResetDeviceFormat(string deviceId);

    [PreserveSig]
    int SetDeviceFormat(string deviceId, nint endpointFormat, nint mixFormat);
}
