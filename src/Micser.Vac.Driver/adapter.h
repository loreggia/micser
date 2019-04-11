#ifndef __ADAPTER_H_
#define __ADAPTER_H_

#include <portcls.h>
#include "sonicsaudio.h"
#include "common.h"
#include "wave.h"

NTSTATUS DriverEntry(
    PDRIVER_OBJECT          DriverObject,
    PUNICODE_STRING         RegistryPathName
);

NTSTATUS AddDevice(
    PDRIVER_OBJECT          DriverObject,
    PDEVICE_OBJECT          PhysicalDeviceObject
);

NTSTATUS StartDevice
(
    PDEVICE_OBJECT          DeviceObject,
    PIRP                    Irp,
    PRESOURCELIST           ResourceList
);

#endif // __ADAPTER_H_