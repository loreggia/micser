/*++

Module Name:

    driver.h

Abstract:

    Shared declarations of the Micser Cable driver (ACX).

    The root device (ROOT\MicserCable) has no circuits itself. Each cable is a child raw PDO (MicserCable\CableNN, instance ID NN =
    cable number) with a render circuit ("Cable N Input") and a capture circuit ("Cable N Output"), which share the cable's ring
    buffer. Cables are added and removed while the device runs (AcxDeviceAddCircuitDevice, AcxDeviceRemoveCircuitDevice), at start
    from the root device's hardware key and later through the control device's IOCTLs (public.h).

--*/

#ifndef _MICSERCABLE_DRIVER_H_
#define _MICSERCABLE_DRIVER_H_

#include <ntddk.h>
#include <windef.h>
#include <mmsystem.h>
#include <ks.h>
#include <ksmedia.h>
#include <ntintsafe.h>
#include <ntstrsafe.h>
#include <wdf.h>
#include <acx.h>

#include "public.h"
#include "cable.h"

#define CABLE_POOLTAG_DRIVER    'DCCM'

#define CABLE_SAMPLE_RATE       48000

#define CableLog(Format, ...) \
    DbgPrintEx(DPFLTR_IHVAUDIO_ID, DPFLTR_ERROR_LEVEL, "MicserCable: " Format "\n", __VA_ARGS__)

//
// Root device: the cables that exist.
//
typedef struct _ROOT_DEVICE_CONTEXT
{
    WDFWAITLOCK         Lock;
    WDFWORKITEM         AddWorkItem;            // adds the pending cables once their previous devices are deleted
    volatile LONG       Started;                // cables are added and removed only while the root device runs
    WDFDEVICE volatile  Cables[MAX_CABLES];     // the cables that were added and not removed since, each with a reference
    ULONG               Channels[MAX_CABLES];   // the layouts of these cables
    volatile LONG       Pending[MAX_CABLES];    // the layout of a cable to add once its previous device is deleted, 0 for none
    KEVENT              Deleted[MAX_CABLES];    // signaled while cable N has no child device, also not one that PnP is still removing
} ROOT_DEVICE_CONTEXT, *PROOT_DEVICE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(ROOT_DEVICE_CONTEXT, GetRootDeviceContext)

typedef struct _CONTROL_DEVICE_CONTEXT
{
    WDFDEVICE       Root;
} CONTROL_DEVICE_CONTEXT, *PCONTROL_DEVICE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(CONTROL_DEVICE_CONTEXT, GetControlDeviceContext)

//
// Cable device (child PDO).
//
typedef struct _CABLE_DEVICE_CONTEXT
{
    PROOT_DEVICE_CONTEXT    Root;
    ULONG                   Number;
    ULONG                   Channels;
    CCable*                 Cable;
    ACXCIRCUIT              Render;
    ACXCIRCUIT              Capture;
    BOOLEAN                 CircuitsAdded;
} CABLE_DEVICE_CONTEXT, *PCABLE_DEVICE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(CABLE_DEVICE_CONTEXT, GetCableDeviceContext)

typedef struct _CIRCUIT_CONTEXT
{
    ULONG           Number;
    BOOLEAN         Render;
    CCable*         Cable;
    volatile LONG   StreamCount;
} CIRCUIT_CONTEXT, *PCIRCUIT_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(CIRCUIT_CONTEXT, GetCircuitContext)

class CCableStream;

typedef struct _STREAM_CONTEXT
{
    CCableStream*       Stream;
    PCIRCUIT_CONTEXT    Circuit;
} STREAM_CONTEXT, *PSTREAM_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(STREAM_CONTEXT, GetStreamContext)

typedef struct _STREAM_TIMER_CONTEXT
{
    CCableStream*   Stream;
} STREAM_TIMER_CONTEXT, *PSTREAM_TIMER_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(STREAM_TIMER_CONTEXT, GetStreamTimerContext)

//
// cabledevice.cpp
//
NTSTATUS Cable_InitializeRoot(_In_ WDFDEVICE Root);
VOID Cable_Start(_In_ WDFDEVICE Root);
VOID Cable_Stop(_In_ WDFDEVICE Root);
NTSTATUS Cable_Apply(_In_ WDFDEVICE Root, _In_ const MICSERCABLE_CONFIG* Config);
VOID Cable_GetActive(_In_ WDFDEVICE Root, _Out_ PULONG ActiveCables);

//
// circuits.cpp
//
NTSTATUS Cable_CreateCircuit(_In_ WDFDEVICE Device, _In_ BOOLEAN Render, _Out_ ACXCIRCUIT* Circuit);

//
// newdelete.cpp
//
PVOID __cdecl operator new(size_t Size, POOL_FLAGS PoolFlags, ULONG Tag);
void __cdecl operator delete(PVOID Pointer, POOL_FLAGS PoolFlags, ULONG Tag);
void __cdecl operator delete(PVOID Pointer);
void __cdecl operator delete(PVOID Pointer, size_t Size);

#endif // _MICSERCABLE_DRIVER_H_
