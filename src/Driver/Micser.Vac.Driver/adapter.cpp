/*
Module Name:
  adapter.cpp

Abstract:
  Setup and miniport installation.  No resources are used by msvad.
*/

// All the GUIDS for all the miniports end up in this object.
#define PUT_GUIDS_HERE

#include "micser.h"
#include "common.h"

//-----------------------------------------------------------------------------
// Defines
//-----------------------------------------------------------------------------
// BUGBUG set this to number of miniports
#define MAX_MINIPORTS 64     // Number of maximum miniports.

// Globals
PDEVICE_OBJECT g_IoDevice;
PDEVICE_OBJECT g_AudioDevice;

//-----------------------------------------------------------------------------
// Referenced forward.
//-----------------------------------------------------------------------------

DRIVER_ADD_DEVICE AddDevice;
NTSTATUS StartDevice(IN PDEVICE_OBJECT, IN PIRP, IN PRESOURCELIST);
NTSTATUS IrpMjCreateHandler(IN PDEVICE_OBJECT, IN PIRP);
NTSTATUS IrpMjCloseHandler(IN PDEVICE_OBJECT, IN PIRP);
NTSTATUS IrpMjDeviceControlHandler(IN PDEVICE_OBJECT, IN PIRP);
NTSTATUS IrpMjPnpHandler(IN PDEVICE_OBJECT, IN PIRP);
VOID DriverUnloadHandler(IN PDRIVER_OBJECT);

//-----------------------------------------------------------------------------
// Functions
//-----------------------------------------------------------------------------

//=============================================================================
#pragma code_seg("INIT")
extern "C" DRIVER_INITIALIZE DriverEntry;
extern "C" NTSTATUS
DriverEntry
(
    IN  PDRIVER_OBJECT          DriverObject,
    IN  PUNICODE_STRING         RegistryPathName
)
{
    /*++

    Routine Description:

      Installable driver initialization entry point.
      This entry point is called directly by the I/O system.

      All audio adapter drivers can use this code without change.

    Arguments:

      DriverObject - pointer to the driver object

      RegistryPath - pointer to a unicode string representing the path,
                       to driver-specific key in the registry.

    Return Value:

      STATUS_SUCCESS if successful,
      STATUS_UNSUCCESSFUL otherwise.

    --*/
    NTSTATUS       ntStatus;

    DPF(D_TERSE, ("[DriverEntry]"));

    // Create the IO device
    ntStatus = IoCreateDevice(
        DriverObject,
        0,
        (PUNICODE_STRING)&IoDeviceName,
        FILE_DEVICE_UNKNOWN,
        0,
        FALSE,
        &g_IoDevice);

    // Create symlink for user mode application access
    if (!NT_SUCCESS(ntStatus))
    {
        return ntStatus;
    }

    ntStatus = IoCreateSymbolicLink((PUNICODE_STRING)&IoDeviceSymLink, (PUNICODE_STRING)&IoDeviceName);

    if (!NT_SUCCESS(ntStatus))
    {
        return ntStatus;
    }

    // Tell the class driver to initialize the driver.
    ntStatus = PcInitializeAdapterDriver(
        DriverObject,
        RegistryPathName,
        (PDRIVER_ADD_DEVICE)AddDevice);

    if (!NT_SUCCESS(ntStatus))
    {
        return ntStatus;
    }

    DriverObject->DriverUnload = DriverUnloadHandler;
#pragma warning (push)
#pragma warning( disable:28169 )
#pragma warning( disable:28023 )
    DriverObject->MajorFunction[IRP_MJ_PNP] = IrpMjPnpHandler;
    DriverObject->MajorFunction[IRP_MJ_CREATE] = IrpMjCreateHandler;
    DriverObject->MajorFunction[IRP_MJ_CLOSE] = IrpMjCloseHandler;
    DriverObject->MajorFunction[IRP_MJ_DEVICE_CONTROL] = IrpMjDeviceControlHandler;
#pragma warning (pop)

    return ntStatus;
} // DriverEntry
#pragma code_seg()

// disable prefast warning 28152 because
// DO_DEVICE_INITIALIZING is cleared in PcAddAdapterDevice
#pragma warning(disable:28152)
#pragma code_seg("PAGE")
//=============================================================================
NTSTATUS AddDevice
(
    IN  PDRIVER_OBJECT          DriverObject,
    IN  PDEVICE_OBJECT          PhysicalDeviceObject
)
/*++

Routine Description:

  The Plug & Play subsystem is handing us a brand new PDO, for which we
  (by means of INF registration) have been asked to provide a driver.

  We need to determine if we need to be in the driver stack for the device.
  Create a function device object to attach to the stack
  Initialize that device object
  Return status success.

  All audio adapter drivers can use this code without change.
  Set MAX_MINIPORTS depending on the number of miniports that the driver
  uses.

Arguments:

  DriverObject - pointer to a driver object

  PhysicalDeviceObject -  pointer to a device object created by the
                            underlying bus driver.

Return Value:

  NT status code.

--*/
{
    PAGED_CODE();

    NTSTATUS ntStatus;

    DPF(D_TERSE, ("[AddDevice]"));

    // Tell the class driver to add the device.
    ntStatus = PcAddAdapterDevice(
        DriverObject,
        PhysicalDeviceObject,
        PCPFNSTARTDEVICE(StartDevice),
        MAX_MINIPORTS,
        0);

    if (!NT_SUCCESS(ntStatus))
    {
        DPF(D_TERSE, ("[PcAddAdapterDevice failed]"));
        return ntStatus;
    }

    return ntStatus;
} // AddDevice

//=============================================================================
NTSTATUS
StartDevice
(
    IN  PDEVICE_OBJECT          DeviceObject,
    IN  PIRP                    Irp,
    IN  PRESOURCELIST           ResourceList
)
{
    /*++

    Routine Description:

      This function is called by the operating system when the device is
      started.
      It is responsible for starting the miniports.  This code is specific to
      the adapter because it calls out miniports for functions that are specific
      to the adapter.

    Arguments:

      DeviceObject - pointer to the driver object

      Irp - pointer to the irp

      ResourceList - pointer to the resource list assigned by PnP manager

    Return Value:

      NT status code.

    --*/
    UNREFERENCED_PARAMETER(ResourceList);
    UNREFERENCED_PARAMETER(Irp);

    PAGED_CODE();

    ASSERT(DeviceObject);
    ASSERT(Irp);
    ASSERT(ResourceList);

    NTSTATUS                    ntStatus = STATUS_SUCCESS;
    PADAPTERCOMMON              pAdapterCommon = NULL;
    PUNKNOWN                    pUnknownCommon = NULL;
    PortClassDeviceContext*     pExtension = static_cast<PortClassDeviceContext*>(DeviceObject->DeviceExtension);

    DPF_ENTER(("[StartDevice]"));

    g_AudioDevice = DeviceObject;

    // create a new adapter common object
    //
    ntStatus =
        NewAdapterCommon
        (
            &pUnknownCommon,
            IID_IAdapterCommon,
            NULL,
            NonPagedPool
        );
    if (NT_SUCCESS(ntStatus))
    {
        ntStatus =
            pUnknownCommon->QueryInterface
            (
                IID_IAdapterCommon,
                (PVOID *)&pAdapterCommon
            );

        if (NT_SUCCESS(ntStatus))
        {
            ntStatus =
                pAdapterCommon->Init(DeviceObject);

            if (NT_SUCCESS(ntStatus))
            {
                // register with PortCls for power-management services
                //
                ntStatus =
                    PcRegisterAdapterPowerManagement
                    (
                        PUNKNOWN(pAdapterCommon),
                        DeviceObject
                    );
            }
        }
    }

    // Tell the adapter common object to instantiate the subdevices.
    //
    if (NT_SUCCESS(ntStatus))
    {
        ntStatus = pAdapterCommon->InstantiateDevices();
    }

    // Stash the adapter common object in the device extension so
    // we can access it for cleanup on stop/removal.
    //
    if (pAdapterCommon)
    {
        pExtension->m_pCommon = pAdapterCommon;
    }

    if (pUnknownCommon)
    {
        pUnknownCommon->Release();
    }

    return ntStatus;
} // StartDevice

VOID DriverUnloadHandler(IN PDRIVER_OBJECT DriverObject)
{
    UNREFERENCED_PARAMETER(DriverObject);
    PAGED_CODE();
    DPF_ENTER(("[DriverUnloadHandler]"));

    IoDeleteSymbolicLink((PUNICODE_STRING)&IoDeviceSymLink);
}

NTSTATUS IrpMjPnpHandler
(
    IN PDEVICE_OBJECT DeviceObject,
    IN PIRP Irp
)
/*++

Routine Description:

  Handles PnP IRPs

Arguments:

  _Fdo - Functional Device object pointer.

  _Irp - The Irp being passed

Return Value:

  NT status code.

--*/
{
    NTSTATUS ntStatus = STATUS_UNSUCCESSFUL;
    PIO_STACK_LOCATION stack;
    PortClassDeviceContext *ext;

    PAGED_CODE();

    ASSERT(DeviceObject);
    ASSERT(Irp);

    DPF_ENTER(("[PnpHandler]"));

    if (DeviceObject == g_IoDevice)
    {
        return STATUS_SUCCESS;
    }

    // Check for the REMOVE_DEVICE irp.  If we're being unloaded,
    // uninstantiate our devices and release the adapter common
    // object.
    //
    stack = IoGetCurrentIrpStackLocation(Irp);

    if ((IRP_MN_REMOVE_DEVICE == stack->MinorFunction) ||
        (IRP_MN_SURPRISE_REMOVAL == stack->MinorFunction) ||
        (IRP_MN_STOP_DEVICE == stack->MinorFunction))
    {
        ext = static_cast<PortClassDeviceContext*>(DeviceObject->DeviceExtension);

        if (ext->m_pCommon != NULL)
        {
            ext->m_pCommon->UninstantiateDevices();
            ext->m_pCommon->Release();
            ext->m_pCommon = NULL;
        }
    }

    ntStatus = PcDispatchIrp(DeviceObject, Irp);

    return ntStatus;
}

NTSTATUS IrpMjCreateHandler(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
    UNREFERENCED_PARAMETER(DeviceObject);

    PAGED_CODE();

    NTSTATUS status = STATUS_SUCCESS;

    DPF_ENTER(("[IrpMjCreateHandler]"));

    if (DeviceObject == g_IoDevice)
    {
        return STATUS_SUCCESS;
    }

    // we did not handle the IRP, dispatch to PortCls
    status = PcDispatchIrp(DeviceObject, Irp);

    return status;
}

NTSTATUS IrpMjCloseHandler(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
    UNREFERENCED_PARAMETER(DeviceObject);

    PAGED_CODE();

    NTSTATUS status = STATUS_SUCCESS;

    DPF_ENTER(("[IrpMjCloseHandler]"));

    if (DeviceObject == g_IoDevice)
    {
        return STATUS_SUCCESS;
    }

    // we did not handle the IRP, dispatch to PortCls
    status = PcDispatchIrp(DeviceObject, Irp);

    return status;
}

NTSTATUS IrpMjDeviceControlHandler(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
    PAGED_CODE();

    ASSERT(DeviceObject);
    ASSERT(Irp);

    NTSTATUS status = STATUS_SUCCESS;
    PIO_STACK_LOCATION pIoStackLocation;
    ULONG ioControlCode;
    PortClassDeviceContext* pExtension;

    DPF_ENTER(("[IrpMjDeviceControlHandler]"));

    // check if we need to handle the IRP
    if (DeviceObject == g_IoDevice)
    {
        pIoStackLocation = IoGetCurrentIrpStackLocation(Irp);
        ioControlCode = pIoStackLocation->Parameters.DeviceIoControl.IoControlCode;

        DPF(D_TERSE, ("Control code received: %i", ioControlCode));

        switch (ioControlCode)
        {
        case IOCTL_RELOAD:
            DPF(D_TERSE, ("IOCTL RELOAD."));
            pExtension = static_cast<PortClassDeviceContext*>(g_AudioDevice->DeviceExtension);

            if (pExtension->m_pCommon == NULL)
            {
                status = STATUS_UNSUCCESSFUL;
                break;
            }

            status = pExtension->m_pCommon->UninstantiateDevices();

            if (!NT_SUCCESS(status))
            {
                break;
            }

            status = pExtension->m_pCommon->InstantiateDevices();
            break;
        }

        Irp->IoStatus.Status = status;
        IoCompleteRequest(Irp, IO_NO_INCREMENT);
        return status;
    }

    // we did not handle the IRP, dispatch to PortCls
    status = PcDispatchIrp(DeviceObject, Irp);

    return status;
}
#pragma code_seg()