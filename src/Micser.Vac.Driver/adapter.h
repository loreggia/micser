#ifndef __ADAPTER_H_
#define __ADAPTER_H_

#include "sonicsaudio.h"
#include "common.h"
#include "trace.h"

// All the GUIDS for all the miniports end up in this object.
#define PUT_GUIDS_HERE

//-----------------------------------------------------------------------------
// Defines
//-----------------------------------------------------------------------------
// BUGBUG set this to number of miniports
#define MAX_MINIPORTS 2     // Number of maximum miniports.

//-----------------------------------------------------------------------------
// Externals
//-----------------------------------------------------------------------------
NTSTATUS CreateMiniportWaveCyclic(
    OUT PUNKNOWN *,
    IN  REFCLSID,
    IN  PUNKNOWN,
    IN  POOL_TYPE
);

NTSTATUS CreateMiniportTopology(
    OUT PUNKNOWN *,
    IN  REFCLSID,
    IN  PUNKNOWN,
    IN  POOL_TYPE
);

//-----------------------------------------------------------------------------
// Referenced forward.
//-----------------------------------------------------------------------------
extern "C" NTSTATUS AddDevice(
    IN  PDRIVER_OBJECT,
    IN  PDEVICE_OBJECT
);

NTSTATUS StartDevice(
    IN  PDEVICE_OBJECT,
    IN  PIRP,
    IN  PRESOURCELIST
);

#endif