/*++

Module Name:

    cabledevice.cpp

Abstract:

    Cable devices: a child raw PDO per cable, added and removed while the root device runs.

--*/

#include "driver.h"

// {4d36e96c-e325-11ce-bfc1-08002be10318}
static const GUID MediaClassGuid = { 0x4d36e96c, 0xe325, 0x11ce, { 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18 } };

// Each cable has a hardware ID of its own (MicserCable\Cable01, ...), so the INF gives each a name of its own: the endpoint names
// contain it ("Cable 1 Input (Micser Cable 1)"), and Windows numbers devices with the same name ("2- ..."). A driver can't set the
// friendly name itself.
DECLARE_CONST_UNICODE_STRING(CableCompatibleId, L"MicserCable\\Cable");
DECLARE_CONST_UNICODE_STRING(CableLocation, L"Micser Cables");

// Like PortCls audio devices: system and administrators all access, everyone else read, write and execute.
DECLARE_CONST_UNICODE_STRING(CableSddl, L"D:P(A;;GA;;;SY)(A;;GA;;;BA)(A;;GRGWGX;;;WD)(A;;GRGWGX;;;RC)");

// How long adding a cable waits for PnP to delete the cable's previous device, which has the same instance ID. An app that keeps
// the device open delays that; the cable is then added once the device is deleted (EvtAddPendingCables).
#define CABLE_DELETE_TIMEOUT_MS 5000

// The reference that Cables holds on a cable device.
#define CABLE_REFERENCE_TAG     ((PVOID)'RCCM')

#pragma code_seg("PAGE")
static NTSTATUS EvtCablePrepareHardware(_In_ WDFDEVICE Device, _In_ WDFCMRESLIST Resources, _In_ WDFCMRESLIST ResourcesTranslated)
{
    PAGED_CODE();
    UNREFERENCED_PARAMETER(Resources);
    UNREFERENCED_PARAMETER(ResourcesTranslated);

    // The circuits are added once and stay until the device is deleted: AcxDeviceRemoveCircuit deletes them, so after a restart of
    // the device (e.g. when Windows installs the INF for it) there would be nothing to add again.
    PCABLE_DEVICE_CONTEXT context = GetCableDeviceContext(Device);
    if (context->CircuitsAdded)
    {
        return STATUS_SUCCESS;
    }

    NTSTATUS status = AcxDeviceAddCircuit(Device, context->Render);
    if (!NT_SUCCESS(status))
    {
        CableLog("AcxDeviceAddCircuit (render) of cable %u failed: 0x%08X", context->Number, status);
        return status;
    }

    status = AcxDeviceAddCircuit(Device, context->Capture);
    if (!NT_SUCCESS(status))
    {
        // the render circuit stays added; a failed start doesn't retry
        CableLog("AcxDeviceAddCircuit (capture) of cable %u failed: 0x%08X", context->Number, status);
        return status;
    }

    context->CircuitsAdded = TRUE;
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
static VOID EvtCableCleanup(_In_ WDFOBJECT Object)
{
    PAGED_CODE();

    PCABLE_DEVICE_CONTEXT context = GetCableDeviceContext((WDFDEVICE)Object);
    if (context->Cable)
    {
        delete context->Cable;
        context->Cable = NULL;
    }

    PROOT_DEVICE_CONTEXT root = context->Root;
    if (!root || context->Number < 1 || context->Number > MAX_CABLES)
    {
        return;
    }

    ULONG index = context->Number - 1;

    // A cable that is still in Cables wasn't removed by Cable_Apply: PnP deletes it because the root device goes away. The root
    // device's lock may already be gone, so this neither waits for it nor lets Cable_Apply add or remove cables any more.
    if (InterlockedCompareExchangePointer((PVOID volatile*)&root->Cables[index], NULL, Object) == Object)
    {
        InterlockedExchange(&root->Started, FALSE);
        WdfObjectDereferenceWithTag((WDFDEVICE)Object, CABLE_REFERENCE_TAG);
    }

    KeSetEvent(&root->Deleted[index], IO_NO_INCREMENT, FALSE);

    if (ReadAcquire(&root->Pending[index]) && ReadAcquire(&root->Started))
    {
        WdfWorkItemEnqueue(root->AddWorkItem);
    }
}

#pragma code_seg("PAGE")
static NTSTATUS CreateCableDevice(_In_ WDFDEVICE Root, _In_ ULONG Number, _In_ ULONG Channels, _Out_ WDFDEVICE* Device)
{
    PAGED_CODE();

    *Device = NULL;

    PWDFDEVICE_INIT deviceInit = WdfPdoInitAllocate(Root);
    if (!deviceInit)
    {
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    WCHAR instanceIdBuffer[8];
    UNICODE_STRING instanceId;
    RtlInitEmptyUnicodeString(&instanceId, instanceIdBuffer, sizeof(instanceIdBuffer));

    WCHAR deviceIdBuffer[32];
    UNICODE_STRING deviceId;
    RtlInitEmptyUnicodeString(&deviceId, deviceIdBuffer, sizeof(deviceIdBuffer));

    WCHAR descriptionBuffer[32];
    UNICODE_STRING description;
    RtlInitEmptyUnicodeString(&description, descriptionBuffer, sizeof(descriptionBuffer));

    NTSTATUS status = RtlUnicodeStringPrintf(&instanceId, L"%02u", Number);
    if (NT_SUCCESS(status))
    {
        status = RtlUnicodeStringPrintf(&deviceId, L"MicserCable\\Cable%02u", Number);
    }

    if (NT_SUCCESS(status))
    {
        status = RtlUnicodeStringPrintf(&description, L"Micser Cable %u", Number);
    }

    if (NT_SUCCESS(status))
    {
        status = WdfPdoInitAssignDeviceID(deviceInit, &deviceId);
    }

    if (NT_SUCCESS(status))
    {
        status = WdfPdoInitAddHardwareID(deviceInit, &deviceId);
    }

    if (NT_SUCCESS(status))
    {
        status = WdfPdoInitAddCompatibleID(deviceInit, &CableCompatibleId);
    }

    if (NT_SUCCESS(status))
    {
        status = WdfPdoInitAssignInstanceID(deviceInit, &instanceId);
    }

    if (NT_SUCCESS(status))
    {
        status = WdfPdoInitAddDeviceText(deviceInit, &description, &CableLocation, 0x409);
    }

    if (NT_SUCCESS(status))
    {
        WdfPdoInitSetDefaultLocale(deviceInit, 0x409);

        // The PDO is the whole stack: the INF installs no function driver for it.
        status = WdfPdoInitAssignRawDevice(deviceInit, &MediaClassGuid);
    }

    if (NT_SUCCESS(status))
    {
        status = WdfDeviceInitAssignSDDLString(deviceInit, &CableSddl);
    }

    if (NT_SUCCESS(status))
    {
        ACX_DEVICEINIT_CONFIG acxInitConfig;
        ACX_DEVICEINIT_CONFIG_INIT(&acxInitConfig);
        acxInitConfig.Flags = AcxDeviceInitConfigRawDevice;
        status = AcxDeviceInitInitialize(deviceInit, &acxInitConfig);
    }

    if (!NT_SUCCESS(status))
    {
        WdfDeviceInitFree(deviceInit);
        return status;
    }

    WDF_PNPPOWER_EVENT_CALLBACKS pnpPowerCallbacks;
    WDF_PNPPOWER_EVENT_CALLBACKS_INIT(&pnpPowerCallbacks);
    pnpPowerCallbacks.EvtDevicePrepareHardware = EvtCablePrepareHardware;
    WdfDeviceInitSetPnpPowerEventCallbacks(deviceInit, &pnpPowerCallbacks);

    WDF_OBJECT_ATTRIBUTES attributes;
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attributes, CABLE_DEVICE_CONTEXT);
    attributes.EvtCleanupCallback = EvtCableCleanup;
    attributes.ExecutionLevel = WdfExecutionLevelPassive;

    PROOT_DEVICE_CONTEXT rootContext = GetRootDeviceContext(Root);
    KeClearEvent(&rootContext->Deleted[Number - 1]);

    WDFDEVICE device;
    status = WdfDeviceCreate(&deviceInit, &attributes, &device);
    if (!NT_SUCCESS(status))
    {
        if (deviceInit)
        {
            WdfDeviceInitFree(deviceInit);
        }

        KeSetEvent(&rootContext->Deleted[Number - 1], IO_NO_INCREMENT, FALSE);
        return status;
    }

    // from here on EvtCableCleanup signals Deleted
    PCABLE_DEVICE_CONTEXT context = GetCableDeviceContext(device);
    context->Root = rootContext;
    context->Number = Number;
    context->Channels = Channels;

    ACX_DEVICE_CONFIG acxConfig;
    ACX_DEVICE_CONFIG_INIT(&acxConfig);
    status = AcxDeviceInitialize(device, &acxConfig);

    if (NT_SUCCESS(status))
    {
        WDF_DEVICE_PNP_CAPABILITIES pnpCaps;
        WDF_DEVICE_PNP_CAPABILITIES_INIT(&pnpCaps);
        pnpCaps.SurpriseRemovalOK = WdfTrue;
        pnpCaps.UINumber = Number;
        WdfDeviceSetPnpCapabilities(device, &pnpCaps);

        context->Cable = new (POOL_FLAG_NON_PAGED, CABLE_POOLTAG_DRIVER) CCable();
        status = context->Cable
            ? context->Cable->Init(Channels > 2 ? CABLE_SURROUND_BUFFER_SIZE : CABLE_BUFFER_SIZE)
            : STATUS_INSUFFICIENT_RESOURCES;
    }

    if (NT_SUCCESS(status))
    {
        status = Cable_CreateCircuit(device, TRUE, &context->Render);
    }

    if (NT_SUCCESS(status))
    {
        status = Cable_CreateCircuit(device, FALSE, &context->Capture);
    }

    if (!NT_SUCCESS(status))
    {
        WdfObjectDelete(device);
        return status;
    }

    *Device = device;
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
static NTSTATUS AddCable(_In_ WDFDEVICE Root, _In_ ULONG Number, _In_ ULONG Channels)
{
    PAGED_CODE();

    PROOT_DEVICE_CONTEXT context = GetRootDeviceContext(Root);

    // PnP removes a cable's device asynchronously; the new one has the same instance ID
    LARGE_INTEGER timeout;
    timeout.QuadPart = -10000LL * CABLE_DELETE_TIMEOUT_MS;
    NTSTATUS status = KeWaitForSingleObject(&context->Deleted[Number - 1], Executive, KernelMode, FALSE, &timeout);
    if (status == STATUS_TIMEOUT)
    {
        return STATUS_DEVICE_BUSY;
    }

    WDFDEVICE cable;
    status = CreateCableDevice(Root, Number, Channels, &cable);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    status = AcxDeviceAddCircuitDevice(Root, cable);
    if (!NT_SUCCESS(status))
    {
        WdfObjectDelete(cable);
        return status;
    }

    WdfObjectReferenceWithTag(cable, CABLE_REFERENCE_TAG);
    context->Channels[Number - 1] = Channels;
    InterlockedExchangePointer((PVOID volatile*)&context->Cables[Number - 1], cable);
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
static NTSTATUS RemoveCable(_In_ WDFDEVICE Root, _In_ ULONG Number)
{
    PAGED_CODE();

    PROOT_DEVICE_CONTEXT context = GetRootDeviceContext(Root);

    // taken out first, so EvtCableCleanup knows that the cable was removed here
    WDFDEVICE cable = (WDFDEVICE)InterlockedExchangePointer((PVOID volatile*)&context->Cables[Number - 1], NULL);
    if (!cable)
    {
        return STATUS_SUCCESS;
    }

    // PnP removes and deletes the device asynchronously; EvtCableCleanup signals Deleted
    NTSTATUS status = AcxDeviceRemoveCircuitDevice(Root, cable);
    if (!NT_SUCCESS(status))
    {
        InterlockedExchangePointer((PVOID volatile*)&context->Cables[Number - 1], cable);
        return status;
    }

    WdfObjectDereferenceWithTag(cable, CABLE_REFERENCE_TAG);
    return STATUS_SUCCESS;
}

// Adds a cable, or marks it pending when its previous device isn't deleted yet.
#pragma code_seg("PAGE")
static NTSTATUS AddOrDeferCable(_In_ WDFDEVICE Root, _In_ ULONG Number, _In_ ULONG Channels)
{
    PAGED_CODE();

    PROOT_DEVICE_CONTEXT context = GetRootDeviceContext(Root);
    InterlockedExchange(&context->Pending[Number - 1], 0);

    NTSTATUS status = AddCable(Root, Number, Channels);
    if (status != STATUS_DEVICE_BUSY)
    {
        return status;
    }

    // EvtCableCleanup queues EvtAddPendingCables when it sees Pending; if it ran before that was set, add the cable here
    InterlockedExchange(&context->Pending[Number - 1], (LONG)Channels);
    if (KeReadStateEvent(&context->Deleted[Number - 1]))
    {
        InterlockedExchange(&context->Pending[Number - 1], 0);
        return AddCable(Root, Number, Channels);
    }

    return STATUS_DEVICE_BUSY;
}

#pragma code_seg("PAGE")
static VOID EvtAddPendingCables(_In_ WDFWORKITEM WorkItem)
{
    PAGED_CODE();

    WDFDEVICE root = (WDFDEVICE)WdfWorkItemGetParentObject(WorkItem);
    PROOT_DEVICE_CONTEXT context = GetRootDeviceContext(root);

    WdfWaitLockAcquire(context->Lock, NULL);

    for (ULONG i = 0; i < MAX_CABLES && ReadAcquire(&context->Started); i++)
    {
        ULONG channels = (ULONG)ReadAcquire(&context->Pending[i]);
        if (channels && !context->Cables[i] && KeReadStateEvent(&context->Deleted[i]))
        {
            InterlockedExchange(&context->Pending[i], 0);
            NTSTATUS status = AddCable(root, i + 1, channels);
            CableLog("add pending cable %u (%u channels): 0x%08X", i + 1, channels, status);
        }
    }

    WdfWaitLockRelease(context->Lock);
}

#pragma code_seg("PAGE")
NTSTATUS Cable_InitializeRoot(_In_ WDFDEVICE Root)
{
    PAGED_CODE();

    PROOT_DEVICE_CONTEXT context = GetRootDeviceContext(Root);
    for (ULONG i = 0; i < MAX_CABLES; i++)
    {
        KeInitializeEvent(&context->Deleted[i], NotificationEvent, TRUE);
    }

    WDF_OBJECT_ATTRIBUTES attributes;
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.ParentObject = Root;
    NTSTATUS status = WdfWaitLockCreate(&attributes, &context->Lock);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    WDF_WORKITEM_CONFIG workItemConfig;
    WDF_WORKITEM_CONFIG_INIT(&workItemConfig, EvtAddPendingCables);
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.ParentObject = Root;
    return WdfWorkItemCreate(&workItemConfig, &attributes, &context->AddWorkItem);
}

#pragma code_seg("PAGE")
VOID Cable_Start(_In_ WDFDEVICE Root)
{
    PAGED_CODE();

    PROOT_DEVICE_CONTEXT context = GetRootDeviceContext(Root);
    WdfWaitLockAcquire(context->Lock, NULL);
    InterlockedExchange(&context->Started, TRUE);
    WdfWaitLockRelease(context->Lock);
}

#pragma code_seg("PAGE")
VOID Cable_Stop(_In_ WDFDEVICE Root)
{
    PAGED_CODE();

    PROOT_DEVICE_CONTEXT context = GetRootDeviceContext(Root);
    WdfWaitLockAcquire(context->Lock, NULL);
    InterlockedExchange(&context->Started, FALSE);
    for (ULONG i = 0; i < MAX_CABLES; i++)
    {
        InterlockedExchange(&context->Pending[i], 0);
    }

    WdfWaitLockRelease(context->Lock);

    WdfWorkItemFlush(context->AddWorkItem);
}

#pragma code_seg("PAGE")
NTSTATUS Cable_Apply(_In_ WDFDEVICE Root, _In_ const MICSERCABLE_CONFIG* Config)
{
    PAGED_CODE();

    PROOT_DEVICE_CONTEXT context = GetRootDeviceContext(Root);
    NTSTATUS result = STATUS_SUCCESS;

    WdfWaitLockAcquire(context->Lock, NULL);

    if (!ReadAcquire(&context->Started))
    {
        WdfWaitLockRelease(context->Lock);
        return STATUS_DEVICE_NOT_READY;
    }

    // first all removals, so their devices go away in parallel
    for (ULONG i = 0; i < MAX_CABLES; i++)
    {
        if (i >= Config->CableCount)
        {
            InterlockedExchange(&context->Pending[i], 0);
        }

        if (context->Cables[i] && (i >= Config->CableCount || context->Channels[i] != Config->Channels[i]))
        {
            NTSTATUS status = RemoveCable(Root, i + 1);
            CableLog("remove cable %u: 0x%08X", i + 1, status);
            if (!NT_SUCCESS(status) && NT_SUCCESS(result))
            {
                result = status;
            }
        }
    }

    for (ULONG i = 0; i < Config->CableCount && ReadAcquire(&context->Started); i++)
    {
        if (!context->Cables[i])
        {
            // a cable that fails stays missing; the others still work
            NTSTATUS status = AddOrDeferCable(Root, i + 1, Config->Channels[i]);
            CableLog("add cable %u (%u channels): 0x%08X", i + 1, Config->Channels[i], status);
            if (!NT_SUCCESS(status) && NT_SUCCESS(result))
            {
                result = status;
            }
        }
    }

    WdfWaitLockRelease(context->Lock);

    return result;
}

#pragma code_seg("PAGE")
VOID Cable_GetActive(_In_ WDFDEVICE Root, _Out_ PULONG ActiveCables)
{
    PAGED_CODE();

    PROOT_DEVICE_CONTEXT context = GetRootDeviceContext(Root);
    ULONG cables = 0;

    WdfWaitLockAcquire(context->Lock, NULL);

    for (ULONG i = 0; i < MAX_CABLES; i++)
    {
        if (context->Cables[i])
        {
            cables |= 1 << i;
        }
    }

    WdfWaitLockRelease(context->Lock);

    *ActiveCables = cables;
}
