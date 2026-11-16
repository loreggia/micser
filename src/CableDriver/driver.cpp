/*++

Module Name:

    driver.cpp

Abstract:

    Driver entry, the root device, the cable configuration in its hardware key and the control device.

--*/

#include "driver.h"

DECLARE_CONST_UNICODE_STRING(ControlDeviceName, MICSERCABLE_DEVICE_NAME);
DECLARE_CONST_UNICODE_STRING(ControlSymbolicLink, MICSERCABLE_SYMBOLIC_LINK);
// System and administrators all access, signed-in users read and write: any user may change the cables.
DECLARE_CONST_UNICODE_STRING(ControlSddl, L"D:P(A;;GA;;;SY)(A;;GA;;;BA)(A;;GRGW;;;AU)");
DECLARE_CONST_UNICODE_STRING(CableCountValueName, CABLE_COUNT_VALUE);

// Only one root device gets the control device.
static WDFDEVICE g_ControlDevice;

extern "C" DRIVER_INITIALIZE DriverEntry;
static EVT_WDF_DRIVER_DEVICE_ADD EvtDeviceAdd;
static EVT_WDF_DEVICE_SELF_MANAGED_IO_INIT EvtDeviceSelfManagedIoInit;
static EVT_WDF_OBJECT_CONTEXT_CLEANUP EvtDeviceCleanup;
static EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL EvtControlDeviceControl;

#pragma code_seg("PAGE")
static NTSTATUS GetChannelsValueName(_In_ ULONG Number, _Out_writes_(Size) PWCHAR Buffer, _In_ size_t Size, _Out_ PUNICODE_STRING Name)
{
    PAGED_CODE();

    NTSTATUS status = RtlStringCchPrintfW(Buffer, Size, CABLE_CHANNELS_VALUE, Number);
    RtlInitUnicodeString(Name, NT_SUCCESS(status) ? Buffer : NULL);
    return status;
}

#pragma code_seg("PAGE")
static VOID ReadConfig(_In_ WDFDEVICE Device, _Out_ PMICSERCABLE_CONFIG Config)
{
    PAGED_CODE();

    RtlZeroMemory(Config, sizeof(*Config));
    Config->CableCount = 1;
    for (ULONG i = 0; i < MAX_CABLES; i++)
    {
        Config->Channels[i] = 2;
    }

    WDFKEY key;
    if (!NT_SUCCESS(WdfDeviceOpenRegistryKey(Device, PLUGPLAY_REGKEY_DEVICE, KEY_READ, WDF_NO_OBJECT_ATTRIBUTES, &key)))
    {
        return;
    }

    ULONG value;
    if (NT_SUCCESS(WdfRegistryQueryULong(key, &CableCountValueName, &value)))
    {
        Config->CableCount = value < 1 ? 1 : value > MAX_CABLES ? MAX_CABLES : value;
    }

    for (ULONG i = 0; i < MAX_CABLES; i++)
    {
        WCHAR buffer[32];
        UNICODE_STRING name;
        if (NT_SUCCESS(GetChannelsValueName(i + 1, buffer, ARRAYSIZE(buffer), &name)) &&
            NT_SUCCESS(WdfRegistryQueryULong(key, &name, &value)) && IsValidCableChannels(value))
        {
            Config->Channels[i] = value;
        }
    }

    WdfRegistryClose(key);
}

#pragma code_seg("PAGE")
static NTSTATUS WriteConfig(_In_ WDFDEVICE Device, _In_ const MICSERCABLE_CONFIG* Config)
{
    PAGED_CODE();

    WDFKEY key;
    NTSTATUS status = WdfDeviceOpenRegistryKey(Device, PLUGPLAY_REGKEY_DEVICE, KEY_WRITE, WDF_NO_OBJECT_ATTRIBUTES, &key);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    status = WdfRegistryAssignULong(key, &CableCountValueName, Config->CableCount);

    // also the layouts of cables above the count, so they keep them when they come back
    for (ULONG i = 0; i < MAX_CABLES && NT_SUCCESS(status); i++)
    {
        WCHAR buffer[32];
        UNICODE_STRING name;
        status = GetChannelsValueName(i + 1, buffer, ARRAYSIZE(buffer), &name);
        if (NT_SUCCESS(status))
        {
            status = WdfRegistryAssignULong(key, &name, Config->Channels[i]);
        }
    }

    WdfRegistryClose(key);
    return status;
}

#pragma code_seg("PAGE")
static BOOLEAN IsValidConfig(_In_ const MICSERCABLE_CONFIG* Config)
{
    PAGED_CODE();

    if (Config->CableCount < 1 || Config->CableCount > MAX_CABLES)
    {
        return FALSE;
    }

    for (ULONG i = 0; i < MAX_CABLES; i++)
    {
        if (!IsValidCableChannels(Config->Channels[i]))
        {
            return FALSE;
        }
    }

    return TRUE;
}

#pragma code_seg("PAGE")
static VOID EvtControlDeviceControl(
    _In_ WDFQUEUE Queue,
    _In_ WDFREQUEST Request,
    _In_ size_t OutputBufferLength,
    _In_ size_t InputBufferLength,
    _In_ ULONG IoControlCode)
{
    PAGED_CODE();
    UNREFERENCED_PARAMETER(OutputBufferLength);
    UNREFERENCED_PARAMETER(InputBufferLength);

    WDFDEVICE root = GetControlDeviceContext(WdfIoQueueGetDevice(Queue))->Root;
    NTSTATUS status;
    ULONG_PTR information = 0;
    PMICSERCABLE_CONFIG buffer;

    switch (IoControlCode)
    {
        case IOCTL_MICSERCABLE_GET_CONFIG:
            status = WdfRequestRetrieveOutputBuffer(Request, sizeof(MICSERCABLE_CONFIG), (PVOID*)&buffer, NULL);
            if (NT_SUCCESS(status))
            {
                ReadConfig(root, buffer);
                Cable_GetActive(root, &buffer->ActiveCables);
                information = sizeof(MICSERCABLE_CONFIG);
            }
            break;

        case IOCTL_MICSERCABLE_SET_CONFIG:
            status = WdfRequestRetrieveInputBuffer(Request, sizeof(MICSERCABLE_CONFIG), (PVOID*)&buffer, NULL);
            if (NT_SUCCESS(status))
            {
                // the buffer is the system's copy; copy it once more so validation and use see the same values
                MICSERCABLE_CONFIG config = *buffer;
                if (!IsValidConfig(&config))
                {
                    status = STATUS_INVALID_PARAMETER;
                }
                else
                {
                    status = WriteConfig(root, &config);
                    if (NT_SUCCESS(status))
                    {
                        status = Cable_Apply(root, &config);
                    }
                }
            }
            break;

        default:
            status = STATUS_INVALID_DEVICE_REQUEST;
            break;
    }

    WdfRequestCompleteWithInformation(Request, status, information);
}

#pragma code_seg("PAGE")
static NTSTATUS CreateControlDevice(_In_ WDFDEVICE Root)
{
    PAGED_CODE();

    PWDFDEVICE_INIT deviceInit = WdfControlDeviceInitAllocate(WdfDeviceGetDriver(Root), &ControlSddl);
    if (!deviceInit)
    {
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    WdfDeviceInitSetExclusive(deviceInit, FALSE);
    NTSTATUS status = WdfDeviceInitAssignName(deviceInit, &ControlDeviceName);
    if (!NT_SUCCESS(status))
    {
        WdfDeviceInitFree(deviceInit);
        return status;
    }

    WDF_OBJECT_ATTRIBUTES attributes;
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attributes, CONTROL_DEVICE_CONTEXT);
    attributes.ExecutionLevel = WdfExecutionLevelPassive;

    WDFDEVICE device;
    status = WdfDeviceCreate(&deviceInit, &attributes, &device);
    if (!NT_SUCCESS(status))
    {
        if (deviceInit)
        {
            WdfDeviceInitFree(deviceInit);
        }

        return status;
    }

    GetControlDeviceContext(device)->Root = Root;

    status = WdfDeviceCreateSymbolicLink(device, &ControlSymbolicLink);
    if (NT_SUCCESS(status))
    {
        // one request at a time, so changes never overlap
        WDF_IO_QUEUE_CONFIG queueConfig;
        WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&queueConfig, WdfIoQueueDispatchSequential);
        queueConfig.EvtIoDeviceControl = EvtControlDeviceControl;
        status = WdfIoQueueCreate(device, &queueConfig, WDF_NO_OBJECT_ATTRIBUTES, NULL);
    }

    if (!NT_SUCCESS(status))
    {
        WdfObjectDelete(device);
        return status;
    }

    WdfControlFinishInitializing(device);
    g_ControlDevice = device;
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
static NTSTATUS EvtDeviceSelfManagedIoInit(_In_ WDFDEVICE Device)
{
    PAGED_CODE();

    Cable_Start(Device);

    MICSERCABLE_CONFIG config;
    ReadConfig(Device, &config);

    // a cable that fails stays missing; the others still work
    (VOID)Cable_Apply(Device, &config);
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
static VOID EvtDeviceSelfManagedIoCleanup(_In_ WDFDEVICE Device)
{
    PAGED_CODE();

    Cable_Stop(Device);
}

#pragma code_seg("PAGE")
static VOID EvtDeviceCleanup(_In_ WDFOBJECT Object)
{
    PAGED_CODE();

    if (g_ControlDevice && GetControlDeviceContext(g_ControlDevice)->Root == (WDFDEVICE)Object)
    {
        WdfObjectDelete(g_ControlDevice);
        g_ControlDevice = NULL;
    }
}

#pragma code_seg("PAGE")
static NTSTATUS EvtDeviceAdd(_In_ WDFDRIVER Driver, _Inout_ PWDFDEVICE_INIT DeviceInit)
{
    PAGED_CODE();
    UNREFERENCED_PARAMETER(Driver);

    ACX_DEVICEINIT_CONFIG acxInitConfig;
    ACX_DEVICEINIT_CONFIG_INIT(&acxInitConfig);
    NTSTATUS status = AcxDeviceInitInitialize(DeviceInit, &acxInitConfig);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    WDF_PNPPOWER_EVENT_CALLBACKS pnpPowerCallbacks;
    WDF_PNPPOWER_EVENT_CALLBACKS_INIT(&pnpPowerCallbacks);
    pnpPowerCallbacks.EvtDeviceSelfManagedIoInit = EvtDeviceSelfManagedIoInit;
    pnpPowerCallbacks.EvtDeviceSelfManagedIoCleanup = EvtDeviceSelfManagedIoCleanup;
    WdfDeviceInitSetPnpPowerEventCallbacks(DeviceInit, &pnpPowerCallbacks);

    WDF_OBJECT_ATTRIBUTES attributes;
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attributes, ROOT_DEVICE_CONTEXT);
    attributes.EvtCleanupCallback = EvtDeviceCleanup;

    WDFDEVICE device;
    status = WdfDeviceCreate(&DeviceInit, &attributes, &device);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    status = Cable_InitializeRoot(device);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    ACX_DEVICE_CONFIG acxConfig;
    ACX_DEVICE_CONFIG_INIT(&acxConfig);
    status = AcxDeviceInitialize(device, &acxConfig);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    WDF_DEVICE_PNP_CAPABILITIES pnpCaps;
    WDF_DEVICE_PNP_CAPABILITIES_INIT(&pnpCaps);
    pnpCaps.SurpriseRemovalOK = WdfTrue;
    WdfDeviceSetPnpCapabilities(device, &pnpCaps);

    if (!g_ControlDevice)
    {
        status = CreateControlDevice(device);
        if (!NT_SUCCESS(status))
        {
            // the cables in the registry still work
            CableLog("creating the control device failed: 0x%08X", status);
        }
    }

    return STATUS_SUCCESS;
}

#pragma code_seg("INIT")
extern "C" NTSTATUS DriverEntry(_In_ PDRIVER_OBJECT DriverObject, _In_ PUNICODE_STRING RegistryPath)
{
    WDF_DRIVER_CONFIG config;
    WDF_DRIVER_CONFIG_INIT(&config, EvtDeviceAdd);

    WDFDRIVER driver;
    NTSTATUS status = WdfDriverCreate(DriverObject, RegistryPath, WDF_NO_OBJECT_ATTRIBUTES, &config, &driver);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    ACX_DRIVER_CONFIG acxConfig;
    ACX_DRIVER_CONFIG_INIT(&acxConfig);
    return AcxDriverInitialize(driver, &acxConfig);
}
