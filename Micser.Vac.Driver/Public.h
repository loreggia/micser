/*++

Module Name:

    public.h

Abstract:

    This module contains the common declarations shared by driver
    and user applications.

Environment:

    user and kernel

--*/

//
// Define an Interface Guid so that apps can find the device and talk to it.
//

DEFINE_GUID (GUID_DEVINTERFACE_MicserVacDriver,
    0x858f45d3,0x94f2,0x42c8,0x8d,0xe8,0x66,0xc5,0x8c,0xba,0x5f,0x08);
// {858f45d3-94f2-42c8-8de8-66c58cba5f08}
