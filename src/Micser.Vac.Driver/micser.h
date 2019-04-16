/*
Module Name:
  micser.h

Abstract:
  Header file for common stuff.
*/

#ifndef __MICSER_H_
#define __MICSER_H_

#include <portcls.h>
#include <stdunk.h>
#include <ksdebug.h>
#include "kshelper.h"

//=============================================================================
// Defines
//=============================================================================

// Version number. Revision numbers are specified for each sample.
#define MICSER_VERSION               1
#define MICSER_REVISION              0

// the maximum number of interfaces/cables
#define MAX_INTERFACES 32

// Product Id
// {C70BB5FA-E8EE-4114-8D44-BF11584EBDF6}
// 0xc70bb5fa, 0xe8ee, 0x4114, 0x8d, 0x44, 0xbf, 0x11, 0x58, 0x4e, 0xbd, 0xf6
DEFINE_GUIDSTRUCT("C70BB5FA-E8EE-4114-8D44-BF11584EBDF6", PID_MICSER);
#define PID_MICSER DEFINE_GUIDNAMED(PID_MICSER);

// Name Guid
// {68E1267A-9254-4EBC-B752-385C99909A81}
// 0x68e1267a, 0x9254, 0x4ebc, 0xb7, 0x52, 0x38, 0x5c, 0x99, 0x90, 0x9a, 0x81);
DEFINE_GUIDSTRUCT("68E1267A-9254-4EBC-B752-385C99909A81", NAME_MICSER);
#define NAME_MICSER DEFINE_GUIDNAMED(NAME_MICSER)

// Pool tag used for MSVAD allocations
#define MICSER_POOLTAG           'MCSR'

// Debug module name
#define STR_MODULENAME              "Micser: "

// Debug utility macros
#define D_FUNC                      4
#define D_BLAB                      DEBUGLVL_BLAB
#define D_VERBOSE                   DEBUGLVL_VERBOSE
#define D_TERSE                     DEBUGLVL_TERSE
#define D_ERROR                     DEBUGLVL_ERROR
#define DPF                         _DbgPrintF
#define DPF_ENTER(x)                DPF(D_FUNC, x)

// Channel orientation
#define CHAN_LEFT                   0
#define CHAN_RIGHT                  1
#define CHAN_MASTER                 (-1)

// Pin properties.
#define MAX_OUTPUT_STREAMS          1       // Number of capture streams.
#define MAX_INPUT_STREAMS           1       // Number of render streams.
#define MAX_TOTAL_STREAMS           MAX_OUTPUT_STREAMS + MAX_INPUT_STREAMS

// PCM Info [6,6,8,32,8000,192000]
#define MIN_CHANNELS                8       // Min Channels.
#define MAX_CHANNELS_PCM            8       // Max Channels.
#define MIN_BITS_PER_SAMPLE_PCM     8       // Min Bits Per Sample
#define MAX_BITS_PER_SAMPLE_PCM     32      // Max Bits Per Sample
#define MIN_SAMPLE_RATE             8000    // Min Sample Rate
#define MAX_SAMPLE_RATE             192000  // Max Sample Rate

// Dma Settings.
#define DMA_BUFFER_SIZE             0x32000
#define TRAN_BUFFER_SIZE            DMA_BUFFER_SIZE

#define KSPROPERTY_TYPE_ALL         KSPROPERTY_TYPE_BASICSUPPORT | \
                                    KSPROPERTY_TYPE_GET | \
                                    KSPROPERTY_TYPE_SET

// Specific node numbers for vrtaupipe.sys
#define DEV_SPECIFIC_VT_BOOL 9
#define DEV_SPECIFIC_VT_I4   10
#define DEV_SPECIFIC_VT_UI4  11

// Safe release.
#define SAFE_RELEASE(p) {if (p) { (p)->Release(); (p) = nullptr; } }

// IO Control
#define SIOCTL_TYPE 666
#define IOCTL_RELOAD CTL_CODE(SIOCTL_TYPE, 0x800, METHOD_BUFFERED, FILE_READ_DATA|FILE_WRITE_DATA)

DECLARE_GLOBAL_CONST_UNICODE_STRING(DeviceName, L"Micser Virtual Audio Cable"); // THIS NEEDS TO BE EQUAL TO THE DEVICE NAME IN THE INF FILE!
DECLARE_GLOBAL_CONST_UNICODE_STRING(IoInterfaceSymLink, L"\\DosDevices\\Micser.Vac.Driver.Device");

//=============================================================================
// Enumerations
//=============================================================================

// Wave pins
enum
{
    KSPIN_WAVE_CAPTURE_SINK = 0,
    KSPIN_WAVE_CAPTURE_SOURCE,
    KSPIN_WAVE_RENDER_SINK,
    KSPIN_WAVE_RENDER_SOURCE
};

// Wave Topology nodes.
enum
{
    KSNODE_WAVE_ADC = 0,
    KSNODE_WAVE_DAC
};

// topology pins.
enum
{
    KSPIN_TOPO_WAVEOUT_SOURCE = 0,
    //KSPIN_TOPO_SYNTHOUT_SOURCE,
    //KSPIN_TOPO_SYNTHIN_SOURCE,
    KSPIN_TOPO_MIC_SOURCE,
    KSPIN_TOPO_LINEOUT_DEST,
    KSPIN_TOPO_WAVEIN_DEST
};

// topology nodes.
enum
{
    KSNODE_TOPO_WAVEOUT_VOLUME = 0,
    KSNODE_TOPO_WAVEOUT_MUTE,
    //KSNODE_TOPO_SYNTHOUT_VOLUME,
    //KSNODE_TOPO_SYNTHOUT_MUTE,
    KSNODE_TOPO_MIC_VOLUME,
    //KSNODE_TOPO_SYNTHIN_VOLUME,
    KSNODE_TOPO_LINEOUT_MIX,
    KSNODE_TOPO_LINEOUT_VOLUME,
    KSNODE_TOPO_WAVEIN_MUX
};

//=============================================================================
// Typedefs
//=============================================================================

// Connection table for registering topology/wave bridge connection
typedef struct _PHYSICALCONNECTIONTABLE {
    ULONG       ulTopologyIn;
    ULONG       ulTopologyOut;
    ULONG       ulWaveIn;
    ULONG       ulWaveOut;
} PHYSICALCONNECTIONTABLE, *PPHYSICALCONNECTIONTABLE;

// This is the structure of the portclass FDO device extension Nt has created
// for us.  We keep the adapter common object here.
struct IAdapterCommon;
typedef struct _PortClassDeviceContext              // 32       64      Byte offsets for 32 and 64 bit architectures
{
    ULONG_PTR m_pulReserved1[2];                    // 0-7      0-15    First two pointers are reserved.
    PDEVICE_OBJECT m_DoNotUsePhysicalDeviceObject;  // 8-11     16-23   Reserved pointer to our Physical Device Object (PDO).
    PVOID m_pvReserved2;                            // 12-15    24-31   Reserved pointer to our Start Device function.
    PVOID m_pvReserved3;                            // 16-19    32-39   "Out Memory" according to DDK.
    IAdapterCommon* m_pCommon;                      // 20-23    40-47   Pointer to our adapter common object.
    PVOID m_pvUnused1;                              // 24-27    48-55   Unused space.
    PVOID m_pvUnused2;                              // 28-31    56-63   Unused space.

    // Anything after above line should not be used.
    // This actually goes on for (64*sizeof(ULONG_PTR)) but it is all opaque.
} PortClassDeviceContext;

//=============================================================================
// Externs
//=============================================================================

// Physical connection table. Defined in mintopo.cpp for each sample
extern PHYSICALCONNECTIONTABLE TopologyPhysicalConnections;

// Generic topology handler
extern NTSTATUS PropertyHandler_Topology(IN PPCPROPERTY_REQUEST PropertyRequest);

// Generic wave port handler
extern NTSTATUS PropertyHandler_Wave(IN PPCPROPERTY_REQUEST PropertyRequest);

// Default WaveFilter automation table.
// Handles the GeneralComponentId request.
extern NTSTATUS PropertyHandler_WaveFilter(IN PPCPROPERTY_REQUEST PropertyRequest);

#endif
