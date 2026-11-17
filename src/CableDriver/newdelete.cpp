/*++

Module Name:

    newdelete.cpp

Abstract:

    Pool allocating new and delete operators.

--*/

#include "driver.h"

#pragma code_seg()
PVOID __cdecl operator new(size_t Size, POOL_FLAGS PoolFlags, ULONG Tag)
{
    return ExAllocatePool2(PoolFlags, Size, Tag);
}

#pragma code_seg()
void __cdecl operator delete(PVOID Pointer, POOL_FLAGS PoolFlags, ULONG Tag)
{
    UNREFERENCED_PARAMETER(PoolFlags);

    if (Pointer)
    {
        ExFreePoolWithTag(Pointer, Tag);
    }
}

#pragma code_seg()
void __cdecl operator delete(PVOID Pointer)
{
    if (Pointer)
    {
        ExFreePool(Pointer);
    }
}

#pragma code_seg()
void __cdecl operator delete(PVOID Pointer, size_t Size)
{
    UNREFERENCED_PARAMETER(Size);

    if (Pointer)
    {
        ExFreePool(Pointer);
    }
}
