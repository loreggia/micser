#include <ntddk.h>
#include <wdf.h>
#include <initguid.h>
#include <guiddef.h>
#include <wdfstring.h>
#include "trace.h"

DEFINE_GUID(INTERFACE_GUID_AUDIO, 0x6994AD04, 0x93EF, 0x11D0, 0xA3, 0xCC, 0x00, 0xA0, 0xC9, 0x22, 0x31, 0x96);
DEFINE_GUID(INTERFACE_GUID_RENDER, 0x65E8773E, 0x8F56, 0x11D0, 0xA3, 0xB9, 0x00, 0xA0, 0xC9, 0x22, 0x31, 0x96);
DEFINE_GUID(INTERFACE_GUID_CAPTURE, 0x65E8773D, 0x8F56, 0x11D0, 0xA3, 0xB9, 0x00, 0xA0, 0xC9, 0x22, 0x31, 0x96);
DEFINE_GUID(INTERFACE_GUID_REALTIME, 0xEB115FFC, 0x10C8, 0x4964, 0x83, 0x1D, 0x6D, 0xCB, 0x02, 0xE6, 0xF2, 0x3F);

DECLARE_UNICODE_STRING_SIZE(InterfaceReferenceString, 20);

EXTERN_C_START

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD MicserVacDriverEvtDeviceAdd;
EVT_WDF_OBJECT_CONTEXT_CLEANUP MicserVacDriverEvtDriverContextCleanup;
NTSTATUS MicserVacDriverCreateDeviceInterface(WDFDEVICE Device, const GUID *InterfaceClassGUID);

EXTERN_C_END
