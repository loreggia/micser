/*
Module Name:
  common.cpp

Abstract:
  Implementation of the AdapterCommon class.
*/

#include "micser.h"
#include "common.h"
#include "hw.h"

//-----------------------------------------------------------------------------
// Externals
//-----------------------------------------------------------------------------
typedef
NTSTATUS
(*PMINIPORTCREATE)
(
    _Out_       PUNKNOWN *,
    _In_        REFCLSID,
    _In_opt_    PUNKNOWN,
    _In_        POOL_TYPE
    );

NTSTATUS
CreateMiniportWaveCyclic
(
    OUT PUNKNOWN *,
    IN  REFCLSID,
    IN  PUNKNOWN,
    _When_((PoolType & NonPagedPoolMustSucceed) != 0,
        __drv_reportError("Must succeed pool allocations are forbidden. "
            "Allocation failures cause a system crash"))
    IN  POOL_TYPE PoolType
);

NTSTATUS
CreateMiniportTopology
(
    OUT PUNKNOWN *,
    IN  REFCLSID,
    IN  PUNKNOWN,
    _When_((PoolType & NonPagedPoolMustSucceed) != 0,
        __drv_reportError("Must succeed pool allocations are forbidden. "
            "Allocation failures cause a system crash"))
    IN  POOL_TYPE PoolType
);

//=============================================================================
// Helper Routines
//=============================================================================
//=============================================================================
#pragma code_seg("PAGE")
NTSTATUS
InstallSubdevice
(
    _In_        PDEVICE_OBJECT          DeviceObject,
    _In_opt_    PIRP                    Irp,
    _In_        PWSTR                   Name,
    _In_        REFGUID                 PortClassId,
    _In_        REFGUID                 MiniportClassId,
    _In_opt_    PMINIPORTCREATE         MiniportCreate,
    _In_opt_    PUNKNOWN                UnknownAdapter,
    _In_opt_    PRESOURCELIST           ResourceList,
    _Out_opt_   PUNKNOWN *              OutPortUnknown,
    _Out_opt_   PUNKNOWN *              OutMiniportUnknown
)
{
    /*++

    Routine Description:

        This function creates and registers a subdevice consisting of a port
        driver, a minport driver and a set of resources bound together.  It will
        also optionally place a pointer to an interface on the port driver in a
        specified location before initializing the port driver.  This is done so
        that a common ISR can have access to the port driver during
        initialization, when the ISR might fire.

    Arguments:

        DeviceObject - pointer to the driver object

        Irp - pointer to the irp object.

        Name - name of the miniport. Passes to PcRegisterSubDevice

        PortClassId - port class id. Passed to PcNewPort.

        MiniportClassId - miniport class id. Passed to PcNewMiniport.

        MiniportCreate - pointer to a miniport creation function. If NULL,
                         PcNewMiniport is used.

        UnknownAdapter - pointer to the adapter object.
                         Used for initializing the port.

        ResourceList - pointer to the resource list.

        OutPortUnknown - pointer to store the unknown port interface.

        OutMiniportUnknown - pointer to store the unknown miniport interface.

    Return Value:

        NT status code.

    --*/
    PAGED_CODE();

    ASSERT(DeviceObject);
    ASSERT(Name);

    NTSTATUS                    ntStatus;
    PPORT                       port = NULL;
    PUNKNOWN                    miniport = NULL;

    DPF_ENTER(("[InstallSubDevice %S]", Name));

    // Create the port driver object
    //
    ntStatus = PcNewPort(&port, PortClassId);

    // Create the miniport object
    //
    if (NT_SUCCESS(ntStatus))
    {
        if (MiniportCreate)
        {
            ntStatus =
                MiniportCreate
                (
                    &miniport,
                    MiniportClassId,
                    NULL,
                    NonPagedPool
                );
        }
        else
        {
            ntStatus =
                PcNewMiniport
                (
                (PMINIPORT *)&miniport,
                    MiniportClassId
                );
        }
    }

    // Init the port driver and miniport in one go.
    //
    if (NT_SUCCESS(ntStatus))
    {
#pragma warning(push)
        // IPort::Init's annotation on ResourceList requires it to be non-NULL.  However,
        // for dynamic devices, we may no longer have the resource list and this should
        // still succeed.
        //
#pragma warning(disable:6387)
        ntStatus =
            port->Init
            (
                DeviceObject,
                Irp,
                miniport,
                UnknownAdapter,
                ResourceList
            );
#pragma warning(pop)

        if (NT_SUCCESS(ntStatus))
        {
            // Register the subdevice (port/miniport combination).
            //
            ntStatus =
                PcRegisterSubdevice
                (
                    DeviceObject,
                    Name,
                    port
                );
        }
    }

    // Deposit the port interfaces if it's needed.
    //
    if (NT_SUCCESS(ntStatus))
    {
        if (OutPortUnknown)
        {
            ntStatus =
                port->QueryInterface
                (
                    IID_IUnknown,
                    (PVOID *)OutPortUnknown
                );
        }

        if (OutMiniportUnknown)
        {
            ntStatus =
                miniport->QueryInterface
                (
                    IID_IUnknown,
                    (PVOID *)OutMiniportUnknown
                );
        }
    }

    if (port)
    {
        port->Release();
    }

    if (miniport)
    {
        miniport->Release();
    }

    return ntStatus;
} // InstallSubDevice

//=============================================================================
// Classes
//=============================================================================

///////////////////////////////////////////////////////////////////////////////
// CAdapterCommon
//

class CAdapterCommon : public IAdapterCommon, public IAdapterPowerManagement, public CUnknown {
private:
    PUNKNOWN                m_pPortWave;            // Port Wave Interface
    PUNKNOWN                m_pMiniportWave;        // Miniport Wave Interface
    PUNKNOWN                m_pPortTopology;        // Port Mixer Topology Interface
    PUNKNOWN                m_pMiniportTopology;    // Miniport Mixer Topology Interface
    PSERVICEGROUP           m_pServiceGroupWave;
    PDEVICE_OBJECT          m_pDeviceObject;
    DEVICE_POWER_STATE      m_PowerState;
    PCMicserHW              m_pHW;                  // Virtual MSVAD HW object
    BOOL                    m_bInstantiated;        // Flag indicating whether or not subdevices are exposed

    //=====================================================================
    // Helper routines for managing the states of topologies being exposed
    STDMETHODIMP_(NTSTATUS) ExposeMixerTopology(IN PWSTR Name);
    STDMETHODIMP_(NTSTATUS) ExposeWaveTopology(IN PWSTR Name);
    STDMETHODIMP_(NTSTATUS) UnexposeMixerTopology();
    STDMETHODIMP_(NTSTATUS) UnexposeWaveTopology();
    STDMETHODIMP_(NTSTATUS) ConnectTopologies();
    STDMETHODIMP_(NTSTATUS) DisconnectTopologies();

public:
    //=====================================================================
    // Default CUnknown
    DECLARE_STD_UNKNOWN();
    DEFINE_STD_CONSTRUCTOR(CAdapterCommon);
    ~CAdapterCommon();

    //=====================================================================
    // Default IAdapterPowerManagement
    IMP_IAdapterPowerManagement;

    //=====================================================================
    // IAdapterCommon methods
    STDMETHODIMP_(NTSTATUS) Init
    (
        IN  PDEVICE_OBJECT  DeviceObject
    );

    STDMETHODIMP_(PDEVICE_OBJECT)   GetDeviceObject(void);
    STDMETHODIMP_(NTSTATUS)         InstantiateDevices(void);
    STDMETHODIMP_(NTSTATUS)         UninstantiateDevices(void);
    STDMETHODIMP_(PUNKNOWN *)       WavePortDriverDest(void);

    STDMETHODIMP_(void) SetWaveServiceGroup(IN PSERVICEGROUP ServiceGroup);
    STDMETHODIMP_(BOOL) bDevSpecificRead();
    STDMETHODIMP_(void) bDevSpecificWrite(IN BOOL bDevSpecific);
    STDMETHODIMP_(INT) iDevSpecificRead();
    STDMETHODIMP_(void) iDevSpecificWrite(IN INT iDevSpecific);
    STDMETHODIMP_(UINT) uiDevSpecificRead();
    STDMETHODIMP_(void) uiDevSpecificWrite(IN UINT uiDevSpecific);
    STDMETHODIMP_(BOOL) MixerMuteRead(IN ULONG Index);
    STDMETHODIMP_(void) MixerMuteWrite(IN ULONG Index, IN BOOL Value);
    STDMETHODIMP_(ULONG) MixerMuxRead(void);
    STDMETHODIMP_(void) MixerMuxWrite(IN ULONG Index);
    STDMETHODIMP_(void) MixerReset(void);
    STDMETHODIMP_(LONG) MixerVolumeRead(IN ULONG Index, IN LONG Channel);
    STDMETHODIMP_(void) MixerVolumeWrite(IN ULONG Index, IN LONG Channel, IN LONG Value);
    STDMETHODIMP_(BOOL) IsInstantiated() { return m_bInstantiated; };

    //=====================================================================
    // friends

    friend NTSTATUS NewAdapterCommon(OUT PADAPTERCOMMON* OutAdapterCommon, IN PRESOURCELIST ResourceList);
};

//-----------------------------------------------------------------------------
// Functions
//-----------------------------------------------------------------------------

//=============================================================================
#pragma code_seg("PAGE")
NTSTATUS
NewAdapterCommon
(
    OUT PUNKNOWN *              Unknown,
    IN  REFCLSID,
    IN  PUNKNOWN                UnknownOuter OPTIONAL,
    _When_((PoolType & NonPagedPoolMustSucceed) != 0,
        __drv_reportError("Must succeed pool allocations are forbidden. "
            "Allocation failures cause a system crash"))
    IN  POOL_TYPE               PoolType
)
/*++

Routine Description:

  Creates a new CAdapterCommon

Arguments:

  Unknown -

  UnknownOuter -

  PoolType

Return Value:

  NT status code.

--*/
{
    PAGED_CODE();

    ASSERT(Unknown);

    STD_CREATE_BODY_
    (
        CAdapterCommon,
        Unknown,
        UnknownOuter,
        PoolType,
        PADAPTERCOMMON
    );
} // NewAdapterCommon

//=============================================================================
CAdapterCommon::~CAdapterCommon
(
    void
)
/*++

Routine Description:

  Destructor for CAdapterCommon.

Arguments:

Return Value:

  void

--*/
{
    PAGED_CODE();

    DPF_ENTER(("[CAdapterCommon::~CAdapterCommon]"));

    if (m_pHW)
    {
        delete m_pHW;
    }

    if (m_pMiniportWave)
    {
        m_pMiniportWave->Release();
        m_pMiniportWave = NULL;
    }

    if (m_pPortWave)
    {
        m_pPortWave->Release();
        m_pPortWave = NULL;
    }

    if (m_pMiniportTopology)
    {
        m_pMiniportTopology->Release();
        m_pMiniportTopology = NULL;
    }

    if (m_pPortTopology)
    {
        m_pPortTopology->Release();
        m_pPortTopology = NULL;
    }

    if (m_pServiceGroupWave)
    {
        m_pServiceGroupWave->Release();
    }
} // ~CAdapterCommon

//=============================================================================
STDMETHODIMP_(PDEVICE_OBJECT)
CAdapterCommon::GetDeviceObject
(
    void
)
/*++

Routine Description:

  Returns the deviceobject

Arguments:

Return Value:

  PDEVICE_OBJECT

--*/
{
    PAGED_CODE();

    return m_pDeviceObject;
} // GetDeviceObject

//=============================================================================
NTSTATUS
CAdapterCommon::Init
(
    IN  PDEVICE_OBJECT          DeviceObject
)
/*++

Routine Description:

    Initialize adapter common object.

Arguments:

    DeviceObject - pointer to the device object

Return Value:

  NT status code.

--*/
{
    PAGED_CODE();

    ASSERT(DeviceObject);

    NTSTATUS                    ntStatus = STATUS_SUCCESS;

    DPF_ENTER(("[CAdapterCommon::Init]"));

    m_pDeviceObject = DeviceObject;
    m_PowerState = PowerDeviceD0;
    m_pPortWave = NULL;
    m_pMiniportWave = NULL;
    m_pPortTopology = NULL;
    m_pMiniportTopology = NULL;
    m_bInstantiated = FALSE;

    // Initialize HW.
    //
    m_pHW = new (NonPagedPool, MICSER_POOLTAG) CMicserHW;
    if (!m_pHW)
    {
        DPF(D_TERSE, ("Insufficient memory for MSVAD HW"));
        ntStatus = STATUS_INSUFFICIENT_RESOURCES;
    }
    else
    {
        m_pHW->MixerReset();
    }

    return ntStatus;
} // Init

//=============================================================================
STDMETHODIMP_(void)
CAdapterCommon::MixerReset
(
    void
)
/*++

Routine Description:

  Reset mixer registers from registry.

Arguments:

Return Value:

  void

--*/
{
    PAGED_CODE();

    if (m_pHW)
    {
        m_pHW->MixerReset();
    }
} // MixerReset

//=============================================================================
STDMETHODIMP
CAdapterCommon::NonDelegatingQueryInterface
(
    _In_         REFIID                      Interface,
    _COM_Outptr_ PVOID *                     Object
)
/*++

Routine Description:

  QueryInterface routine for AdapterCommon

Arguments:

  Interface -

  Object -

Return Value:

  NT status code.

--*/
{
    PAGED_CODE();

    ASSERT(Object);

    if (IsEqualGUIDAligned(Interface, IID_IUnknown))
    {
        *Object = PVOID(PUNKNOWN(PADAPTERCOMMON(this)));
    }
    else if (IsEqualGUIDAligned(Interface, IID_IAdapterCommon))
    {
        *Object = PVOID(PADAPTERCOMMON(this));
    }
    else if (IsEqualGUIDAligned(Interface, IID_IAdapterPowerManagement))
    {
        *Object = PVOID(PADAPTERPOWERMANAGEMENT(this));
    }
    else
    {
        *Object = NULL;
    }

    if (*Object)
    {
        PUNKNOWN(*Object)->AddRef();
        return STATUS_SUCCESS;
    }

    return STATUS_INVALID_PARAMETER;
} // NonDelegatingQueryInterface

//=============================================================================
STDMETHODIMP_(void)
CAdapterCommon::SetWaveServiceGroup
(
    IN PSERVICEGROUP            ServiceGroup
)
/*++

Routine Description:

Arguments:

Return Value:

  NT status code.

--*/
{
    PAGED_CODE();

    DPF_ENTER(("[CAdapterCommon::SetWaveServiceGroup]"));

    if (m_pServiceGroupWave)
    {
        m_pServiceGroupWave->Release();
    }

    m_pServiceGroupWave = ServiceGroup;

    if (m_pServiceGroupWave)
    {
        m_pServiceGroupWave->AddRef();
    }
} // SetWaveServiceGroup

//=============================================================================
STDMETHODIMP_(NTSTATUS)
CAdapterCommon::InstantiateDevices
(
    void
)
/*++

Routine Description:

  Instantiates the wave and topology ports and exposes them.

Arguments:

Return Value:

  NTSTATUS

--*/
{
    PAGED_CODE();

    NTSTATUS ntStatus = STATUS_SUCCESS;

    if (m_bInstantiated)
    {
        return STATUS_SUCCESS;
    }

    // If the mixer topology port is not exposed, create and expose it.
    //
    ntStatus = ExposeMixerTopology(L"1");

    // Create and expose the wave topology.
    //
    if (NT_SUCCESS(ntStatus))
    {
        ntStatus = ExposeWaveTopology(L"2");
    }

    // Register the physical connection between wave and mixer topologies.
    //
    if (NT_SUCCESS(ntStatus))
    {
        ntStatus = ConnectTopologies();
    }

    if (NT_SUCCESS(ntStatus))
    {
        m_bInstantiated = TRUE;
    }

    return ntStatus;
} // InstantiateDevices

//=============================================================================
STDMETHODIMP_(NTSTATUS)
CAdapterCommon::UninstantiateDevices
(
    void
)
/*++

Routine Description:

  Uninstantiates the wave and topology ports.

Arguments:

Return Value:

  NTSTATUS

--*/
{
    PAGED_CODE();

    NTSTATUS ntStatus = STATUS_SUCCESS;

    // Check if we're already uninstantiated
    //
    if (!m_bInstantiated)
    {
        return ntStatus;
    }

    // Unregister the physical connection between wave and mixer topologies.
    //
    if (NT_SUCCESS(ntStatus))
    {
        ntStatus = DisconnectTopologies();
    }

    // Unregister and destroy the wave port
    //
    if (NT_SUCCESS(ntStatus))
    {
        ntStatus = UnexposeWaveTopology();
    }

    // Unregister the topo port
    //
    if (NT_SUCCESS(ntStatus))
    {
        ntStatus = UnexposeMixerTopology();
    }

    if (NT_SUCCESS(ntStatus))
    {
        m_bInstantiated = FALSE;
    }

    return ntStatus;
} // UninstantiateDevices

//=============================================================================
STDMETHODIMP_(NTSTATUS)
CAdapterCommon::ExposeMixerTopology
(
    IN PWSTR Name
)
/*++

Routine Description:

  Creates and registers the mixer topology.

Arguments:

Return Value:

  NTSTATUS

--*/
{
    NTSTATUS ntStatus = STATUS_SUCCESS;

    PAGED_CODE();

    if (m_pPortTopology)
    {
        return ntStatus;
    }

    ntStatus = InstallSubdevice(
        m_pDeviceObject,
        NULL,
        (L"Topology%s", Name),
        CLSID_PortTopology,
        CLSID_PortTopology,
        CreateMiniportTopology,
        PUNKNOWN(PADAPTERCOMMON(this)),
        NULL,
        &m_pPortTopology,
        &m_pMiniportTopology);

    return ntStatus;
}

STDMETHODIMP_(NTSTATUS)
CAdapterCommon::ExposeWaveTopology
(
    IN PWSTR Name
)
/*++

Routine Description:

  Creates and registers wave topology.

Arguments:

Return Value:

  NTSTATUS

--*/
{
    NTSTATUS ntStatus = STATUS_SUCCESS;

    PAGED_CODE();

    if (m_pPortWave)
    {
        return ntStatus;
    }

    ntStatus = InstallSubdevice(
        m_pDeviceObject,
        NULL,
        (L"Wave%s", Name),
        CLSID_PortWaveCyclic,
        CLSID_PortWaveCyclic,
        CreateMiniportWaveCyclic,
        PUNKNOWN(PADAPTERCOMMON(this)),
        NULL,
        &m_pPortWave,
        &m_pMiniportWave);

    return ntStatus;
}

STDMETHODIMP_(NTSTATUS)
CAdapterCommon::UnexposeMixerTopology
(
    void
)
/*++

Routine Description:

  Unregisters and releases the mixer topology.

Arguments:

Return Value:

  NTSTATUS

--*/
{
    NTSTATUS                        ntStatus = STATUS_SUCCESS;
    PUNREGISTERSUBDEVICE            pUnregisterSubdevice = NULL;

    PAGED_CODE();

    if (NULL == m_pPortTopology)
    {
        return ntStatus;
    }

    // Get the IUnregisterSubdevice interface.
    //
    ntStatus = m_pPortTopology->QueryInterface(IID_IUnregisterSubdevice,
        (PVOID *)&pUnregisterSubdevice);

    // Unregister the topo port.
    //
    if (NT_SUCCESS(ntStatus))
    {
        ntStatus = pUnregisterSubdevice->UnregisterSubdevice(
            m_pDeviceObject,
            m_pPortTopology);

        // Release the IUnregisterSubdevice interface.
        //
        pUnregisterSubdevice->Release();

        // At this point, we're done with the mixer topology and
        // the miniport.
        //
        if (NT_SUCCESS(ntStatus))
        {
            m_pPortTopology->Release();
            m_pPortTopology = NULL;

            m_pMiniportTopology->Release();
            m_pMiniportTopology = NULL;
        }
    }

    return ntStatus;
}

STDMETHODIMP_(NTSTATUS)
CAdapterCommon::UnexposeWaveTopology
(
    void
)
/*++

Routine Description:

  Unregisters and releases the wave topology.

Arguments:

Return Value:

  NTSTATUS

--*/
{
    NTSTATUS                        ntStatus = STATUS_SUCCESS;
    PUNREGISTERSUBDEVICE            pUnregisterSubdevice = NULL;

    PAGED_CODE();

    if (NULL == m_pPortWave)
    {
        return ntStatus;
    }

    // Get the IUnregisterSubdevice interface.
    //
    ntStatus = m_pPortWave->QueryInterface(IID_IUnregisterSubdevice,
        (PVOID *)&pUnregisterSubdevice);

    // Unregister the wave port.
    //
    if (NT_SUCCESS(ntStatus))
    {
        ntStatus = pUnregisterSubdevice->UnregisterSubdevice(
            m_pDeviceObject,
            m_pPortWave);

        // Release the IUnregisterSubdevice interface.
        //
        pUnregisterSubdevice->Release();

        // At this point, we're done with the mixer topology and
        // the miniport.
        //
        if (NT_SUCCESS(ntStatus))
        {
            m_pPortWave->Release();
            m_pPortWave = NULL;

            m_pMiniportWave->Release();
            m_pMiniportWave = NULL;
        }
    }
    return ntStatus;
}

STDMETHODIMP_(NTSTATUS)
CAdapterCommon::ConnectTopologies
(
    void
)
/*++

Routine Description:

  Connects the bridge pins between the wave and mixer topologies.

Arguments:

Return Value:

  NTSTATUS

--*/
{
    NTSTATUS ntStatus = STATUS_SUCCESS;

    PAGED_CODE();

    // Connect the capture path.
    //
    if ((TopologyPhysicalConnections.ulTopologyOut != (ULONG)-1) &&
        (TopologyPhysicalConnections.ulWaveIn != (ULONG)-1))
    {
        ntStatus = PcRegisterPhysicalConnection(
            m_pDeviceObject,
            m_pPortTopology,
            TopologyPhysicalConnections.ulTopologyOut,
            m_pPortWave,
            TopologyPhysicalConnections.ulWaveIn);
    }

    // Connect the render path.
    //
    if (NT_SUCCESS(ntStatus))
    {
        if ((TopologyPhysicalConnections.ulWaveOut != (ULONG)-1) &&
            (TopologyPhysicalConnections.ulTopologyIn != (ULONG)-1))
        {
            ntStatus = PcRegisterPhysicalConnection(
                m_pDeviceObject,
                m_pPortWave,
                TopologyPhysicalConnections.ulWaveOut,
                m_pPortTopology,
                TopologyPhysicalConnections.ulTopologyIn
            );
        }
    }

    return ntStatus;
}

STDMETHODIMP_(NTSTATUS)
CAdapterCommon::DisconnectTopologies
(
    void
)
/*++

Routine Description:

  Disconnects the bridge pins between the wave and mixer topologies.

Arguments:

Return Value:

  NTSTATUS

--*/
{
    NTSTATUS                        ntStatus = STATUS_SUCCESS;
    NTSTATUS                        ntStatus2 = STATUS_SUCCESS;
    PUNREGISTERPHYSICALCONNECTION   pUnregisterPhysicalConnection = NULL;

    PAGED_CODE();

    //
    // Get the IUnregisterPhysicalConnection interface
    //
    ntStatus = m_pPortTopology->QueryInterface(IID_IUnregisterPhysicalConnection,
        (PVOID *)&pUnregisterPhysicalConnection);
    if (NT_SUCCESS(ntStatus))
    {
        //
        // Remove the render physical connection
        //
        if ((TopologyPhysicalConnections.ulWaveOut != (ULONG)-1) &&
            (TopologyPhysicalConnections.ulTopologyIn != (ULONG)-1))
        {
            ntStatus = pUnregisterPhysicalConnection->UnregisterPhysicalConnection(
                m_pDeviceObject,
                m_pPortWave,
                TopologyPhysicalConnections.ulWaveOut,
                m_pPortTopology,
                TopologyPhysicalConnections.ulTopologyIn);

            if (!NT_SUCCESS(ntStatus))
            {
                DPF(D_TERSE, ("DisconnectTopologies: UnregisterPhysicalConnection(render) failed, 0x%x", ntStatus));
            }
        }

        //
        // Remove the capture physical connection
        //
        if ((TopologyPhysicalConnections.ulTopologyOut != (ULONG)-1) &&
            (TopologyPhysicalConnections.ulWaveIn != (ULONG)-1))
        {
            ntStatus2 = pUnregisterPhysicalConnection->UnregisterPhysicalConnection(
                m_pDeviceObject,
                m_pPortTopology,
                TopologyPhysicalConnections.ulTopologyOut,
                m_pPortWave,
                TopologyPhysicalConnections.ulWaveIn);

            if (!NT_SUCCESS(ntStatus2))
            {
                DPF(D_TERSE, ("DisconnectTopologies: UnregisterPhysicalConnection(capture) failed, 0x%x", ntStatus2));
                if (NT_SUCCESS(ntStatus))
                {
                    ntStatus = ntStatus2;
                }
            }
        }
    }

    SAFE_RELEASE(pUnregisterPhysicalConnection);

    return ntStatus;
}

//=============================================================================
STDMETHODIMP_(PUNKNOWN *)
CAdapterCommon::WavePortDriverDest
(
    void
)
/*++

Routine Description:

  Returns the wave port.

Arguments:

Return Value:

  PUNKNOWN : pointer to waveport

--*/
{
    PAGED_CODE();

    return &m_pPortWave;
} // WavePortDriverDest
#pragma code_seg()

//=============================================================================
STDMETHODIMP_(BOOL)
CAdapterCommon::bDevSpecificRead()
/*++

Routine Description:

  Fetch Device Specific information.

Arguments:

  N/A

Return Value:

    BOOL - Device Specific info

--*/
{
    if (m_pHW)
    {
        return m_pHW->bGetDevSpecific();
    }

    return FALSE;
} // bDevSpecificRead

//=============================================================================
STDMETHODIMP_(void)
CAdapterCommon::bDevSpecificWrite
(
    IN  BOOL                    bDevSpecific
)
/*++

Routine Description:

  Store the new value in the Device Specific location.

Arguments:

  bDevSpecific - Value to store

Return Value:

  N/A.

--*/
{
    if (m_pHW)
    {
        m_pHW->bSetDevSpecific(bDevSpecific);
    }
} // DevSpecificWrite

//=============================================================================
STDMETHODIMP_(INT)
CAdapterCommon::iDevSpecificRead()
/*++

Routine Description:

  Fetch Device Specific information.

Arguments:

  N/A

Return Value:

    INT - Device Specific info

--*/
{
    if (m_pHW)
    {
        return m_pHW->iGetDevSpecific();
    }

    return 0;
} // iDevSpecificRead

//=============================================================================
STDMETHODIMP_(void)
CAdapterCommon::iDevSpecificWrite
(
    IN  INT                    iDevSpecific
)
/*++

Routine Description:

  Store the new value in the Device Specific location.

Arguments:

  iDevSpecific - Value to store

Return Value:

  N/A.

--*/
{
    if (m_pHW)
    {
        m_pHW->iSetDevSpecific(iDevSpecific);
    }
} // iDevSpecificWrite

//=============================================================================
STDMETHODIMP_(UINT)
CAdapterCommon::uiDevSpecificRead()
/*++

Routine Description:

  Fetch Device Specific information.

Arguments:

  N/A

Return Value:

    UINT - Device Specific info

--*/
{
    if (m_pHW)
    {
        return m_pHW->uiGetDevSpecific();
    }

    return 0;
} // uiDevSpecificRead

//=============================================================================
STDMETHODIMP_(void)
CAdapterCommon::uiDevSpecificWrite
(
    IN  UINT                    uiDevSpecific
)
/*++

Routine Description:

  Store the new value in the Device Specific location.

Arguments:

  uiDevSpecific - Value to store

Return Value:

  N/A.

--*/
{
    if (m_pHW)
    {
        m_pHW->uiSetDevSpecific(uiDevSpecific);
    }
} // uiDevSpecificWrite

//=============================================================================
STDMETHODIMP_(BOOL)
CAdapterCommon::MixerMuteRead
(
    IN  ULONG                   Index
)
/*++

Routine Description:

  Store the new value in mixer register array.

Arguments:

  Index - node id

Return Value:

    BOOL - mixer mute setting for this node

--*/
{
    if (m_pHW)
    {
        return m_pHW->GetMixerMute(Index);
    }

    return 0;
} // MixerMuteRead

//=============================================================================
STDMETHODIMP_(void)
CAdapterCommon::MixerMuteWrite
(
    IN  ULONG                   Index,
    IN  BOOL                    Value
)
/*++

Routine Description:

  Store the new value in mixer register array.

Arguments:

  Index - node id

  Value - new mute settings

Return Value:

  NT status code.

--*/
{
    if (m_pHW)
    {
        m_pHW->SetMixerMute(Index, Value);
    }
} // MixerMuteWrite

//=============================================================================
STDMETHODIMP_(ULONG)
CAdapterCommon::MixerMuxRead()
/*++

Routine Description:

  Return the mux selection

Arguments:

  Index - node id

  Value - new mute settings

Return Value:

  NT status code.

--*/
{
    if (m_pHW)
    {
        return m_pHW->GetMixerMux();
    }

    return 0;
} // MixerMuxRead

//=============================================================================
STDMETHODIMP_(void)
CAdapterCommon::MixerMuxWrite
(
    IN  ULONG                   Index
)
/*++

Routine Description:

  Store the new mux selection

Arguments:

  Index - node id

  Value - new mute settings

Return Value:

  NT status code.

--*/
{
    if (m_pHW)
    {
        m_pHW->SetMixerMux(Index);
    }
} // MixerMuxWrite

//=============================================================================
STDMETHODIMP_(LONG)
CAdapterCommon::MixerVolumeRead
(
    IN  ULONG                   Index,
    IN  LONG                    Channel
)
/*++

Routine Description:

  Return the value in mixer register array.

Arguments:

  Index - node id

  Channel = which channel

Return Value:

    Byte - mixer volume settings for this line

--*/
{
    if (m_pHW)
    {
        return m_pHW->GetMixerVolume(Index, Channel);
    }

    return 0;
} // MixerVolumeRead

//=============================================================================
STDMETHODIMP_(void)
CAdapterCommon::MixerVolumeWrite
(
    IN  ULONG                   Index,
    IN  LONG                    Channel,
    IN  LONG                    Value
)
/*++

Routine Description:

  Store the new value in mixer register array.

Arguments:

  Index - node id

  Channel - which channel

  Value - new volume level

Return Value:

    void

--*/
{
    if (m_pHW)
    {
        m_pHW->SetMixerVolume(Index, Channel, Value);
    }
} // MixerVolumeWrite

//=============================================================================
STDMETHODIMP_(void)
CAdapterCommon::PowerChangeState
(
    IN  POWER_STATE             NewState
)
/*++

Routine Description:

Arguments:

  NewState - The requested, new power state for the device.

Return Value:

    void

--*/
{
    //UINT i;

    DPF_ENTER(("[CAdapterCommon::PowerChangeState]"));

    // is this actually a state change??
    //
    if (NewState.DeviceState != m_PowerState)
    {
        // switch on new state
        //
        switch (NewState.DeviceState)
        {
        case PowerDeviceD0:
        case PowerDeviceD1:
        case PowerDeviceD2:
        case PowerDeviceD3:
            m_PowerState = NewState.DeviceState;

            DPF
            (
                D_VERBOSE,
                ("Entering D%d", ULONG(m_PowerState) - ULONG(PowerDeviceD0))
            );

            break;

        default:

            DPF(D_VERBOSE, ("Unknown Device Power State"));
            break;
        }
    }
} // PowerStateChange

//=============================================================================
STDMETHODIMP_(NTSTATUS)
CAdapterCommon::QueryDeviceCapabilities
(
    IN  PDEVICE_CAPABILITIES    PowerDeviceCaps
)
/*++

Routine Description:

    Called at startup to get the caps for the device.  This structure provides
    the system with the mappings between system power state and device power
    state.  This typically will not need modification by the driver.

Arguments:

  PowerDeviceCaps - The device's capabilities.

Return Value:

  NT status code.

--*/
{
    UNREFERENCED_PARAMETER(PowerDeviceCaps);

    DPF_ENTER(("[CAdapterCommon::QueryDeviceCapabilities]"));

    return (STATUS_SUCCESS);
} // QueryDeviceCapabilities

//=============================================================================
STDMETHODIMP_(NTSTATUS)
CAdapterCommon::QueryPowerChangeState
(
    IN  POWER_STATE             NewStateQuery
)
/*++

Routine Description:

  Query to see if the device can change to this power state

Arguments:

  NewStateQuery - The requested, new power state for the device

Return Value:

  NT status code.

--*/
{
    UNREFERENCED_PARAMETER(NewStateQuery);
    DPF_ENTER(("[CAdapterCommon::QueryPowerChangeState]"));

    return STATUS_SUCCESS;
} // QueryPowerChangeState