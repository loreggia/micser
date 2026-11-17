/*++

Module Name:

    public.h

Abstract:

    Control interface of the Micser Cable driver: \\.\MicserCable takes these IOCTLs from any signed-in user. DriverUtility
    (CableControl.cs) mirrors the codes and the structure.

--*/

#ifndef _MICSERCABLE_PUBLIC_H_
#define _MICSERCABLE_PUBLIC_H_

#define MICSERCABLE_DEVICE_NAME     L"\\Device\\MicserCable"
#define MICSERCABLE_SYMBOLIC_LINK   L"\\DosDevices\\MicserCable"

#define MAX_CABLES                  16

#define FILE_DEVICE_MICSERCABLE     0x8000

// The cables: CableCount cables numbered 1 to CableCount, cable N with Channels[N - 1] channels (2, 6 or 8). In the output of
// GET_CONFIG, bit N - 1 of ActiveCables is set while cable N's device exists; SET_CONFIG ignores it.
typedef struct _MICSERCABLE_CONFIG
{
    ULONG CableCount;
    ULONG Channels[MAX_CABLES];
    ULONG ActiveCables;
} MICSERCABLE_CONFIG, *PMICSERCABLE_CONFIG;

// Output: MICSERCABLE_CONFIG.
#define IOCTL_MICSERCABLE_GET_CONFIG    CTL_CODE(FILE_DEVICE_MICSERCABLE, 0x800, METHOD_BUFFERED, FILE_READ_ACCESS)

// Input: MICSERCABLE_CONFIG. Keeps it in the root device's hardware key, removes the cables above the count, creates the devices of
// the cables whose layout changed again and adds the new cables. Streams of a removed cable fail from then on.
#define IOCTL_MICSERCABLE_SET_CONFIG    CTL_CODE(FILE_DEVICE_MICSERCABLE, 0x801, METHOD_BUFFERED, FILE_WRITE_ACCESS)

#endif // _MICSERCABLE_PUBLIC_H_
