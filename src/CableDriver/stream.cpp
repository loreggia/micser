/*++

Module Name:

    stream.cpp

Abstract:

    Cable streams: creation and the RT packet engine. Packet allocation and the stream callbacks follow Microsoft's ACX AudioCodec
    sample (Windows-driver-samples, Microsoft Public License: License-MS-PL.txt).

--*/

#include "driver.h"
#include "stream.h"

#define TIMER_PERIOD_MS 1

#pragma code_seg("PAGE")
CCableStream::CCableStream(_In_ ACXSTREAM Stream, _In_ ACXDATAFORMAT Format, _In_ CCable* Cable, _In_ BOOLEAN Render)
    : m_Stream(Stream),
      m_Cable(Cable),
      m_Render(Render),
      m_CopyProtect(0),
      m_PacketCount(0),
      m_PacketSize(0),
      m_FirstPacketOffset(0),
      m_Timer(NULL),
      m_State(AcxStreamStateStop),
      m_CurrentPacket(0),
      m_StartQpc(0),
      m_StartPosition(0),
      m_Processed(0)
{
    PAGED_CODE();

    m_BytesPerSecond = AcxDataFormatGetAverageBytesPerSec(Format);
    m_BlockAlign = AcxDataFormatGetBlockAlign(Format);
    m_Channels = AcxDataFormatGetChannelsCount(Format);
    m_BytesPerSample = m_BlockAlign / m_Channels;
    m_CurrentPacketStart.QuadPart = 0;
    m_LastPacketStart.QuadPart = 0;
    KeInitializeSpinLock(&m_Lock);
    KeQueryPerformanceCounter(&m_QpcFrequency);
    RtlZeroMemory(m_Packets, sizeof(m_Packets));
}

#pragma code_seg("PAGE")
NTSTATUS CCableStream::AllocateRtPackets(_In_ ULONG PacketCount, _In_ ULONG PacketSize, _Out_ PACX_RTPACKET* Packets)
{
    PAGED_CODE();

    *Packets = NULL;

    if (PacketCount == 0 || PacketCount > MAX_PACKET_COUNT || PacketSize == 0 || PacketSize % m_BlockAlign != 0)
    {
        return STATUS_INVALID_PARAMETER;
    }

    PACX_RTPACKET packets =
        (PACX_RTPACKET)ExAllocatePool2(POOL_FLAG_NON_PAGED, sizeof(ACX_RTPACKET) * PacketCount, CABLE_POOLTAG_DRIVER);
    if (!packets)
    {
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    // Whole pages, so no other kernel memory is mapped into the client. Packet 0 ends on a page boundary, packet 1 starts on one.
    ULONG allocSize;
    NTSTATUS status = RtlULongAdd(PacketSize, PAGE_SIZE - 1, &allocSize);
    if (!NT_SUCCESS(status))
    {
        ExFreePoolWithTag(packets, CABLE_POOLTAG_DRIVER);
        return status;
    }

    allocSize &= ~(PAGE_SIZE - 1);

    for (ULONG i = 0; i < PacketCount; i++)
    {
        ACX_RTPACKET_INIT(&packets[i]);

        PVOID buffer = ExAllocatePool2(POOL_FLAG_NON_PAGED, allocSize, CABLE_POOLTAG_DRIVER);
        PMDL mdl = buffer ? IoAllocateMdl(buffer, allocSize, FALSE, FALSE, NULL) : NULL;
        if (!mdl)
        {
            if (buffer)
            {
                ExFreePoolWithTag(buffer, CABLE_POOLTAG_DRIVER);
            }

            FreeRtPackets(packets, i);
            return STATUS_INSUFFICIENT_RESOURCES;
        }

        MmBuildMdlForNonPagedPool(mdl);
        WDF_MEMORY_DESCRIPTOR_INIT_MDL(&packets[i].RtPacketBuffer, mdl, allocSize);
        packets[i].RtPacketSize = PacketSize;
        packets[i].RtPacketOffset = i == 0 ? allocSize - PacketSize : 0;
        m_Packets[i] = buffer;
    }

    m_PacketCount = PacketCount;
    m_PacketSize = PacketSize;
    m_FirstPacketOffset = allocSize - PacketSize;
    *Packets = packets;
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
VOID CCableStream::FreeRtPackets(_In_ PACX_RTPACKET Packets, _In_ ULONG PacketCount)
{
    PAGED_CODE();

    for (ULONG i = 0; i < PacketCount; i++)
    {
        PMDL mdl = Packets[i].RtPacketBuffer.u.MdlType.Mdl;
        if (mdl)
        {
            PVOID buffer = MmGetMdlVirtualAddress(mdl);
            IoFreeMdl(mdl);
            ExFreePoolWithTag(buffer, CABLE_POOLTAG_DRIVER);
        }

        if (i < MAX_PACKET_COUNT)
        {
            m_Packets[i] = NULL;
        }
    }

    ExFreePoolWithTag(Packets, CABLE_POOLTAG_DRIVER);
}

#pragma code_seg("PAGE")
NTSTATUS CCableStream::PrepareHardware()
{
    PAGED_CODE();

    if (m_State == AcxStreamStatePause)
    {
        return STATUS_SUCCESS;
    }

    if (m_State != AcxStreamStateStop)
    {
        return STATUS_INVALID_STATE_TRANSITION;
    }

    WDF_TIMER_CONFIG timerConfig;
    WDF_TIMER_CONFIG_INIT_PERIODIC(&timerConfig, CCableStream::EvtTimer, TIMER_PERIOD_MS);
    timerConfig.AutomaticSerialization = FALSE;
    timerConfig.UseHighResolutionTimer = WdfTrue;

    WDF_OBJECT_ATTRIBUTES attributes;
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attributes, STREAM_TIMER_CONTEXT);
    attributes.ParentObject = m_Stream;

    NTSTATUS status = WdfTimerCreate(&timerConfig, &attributes, &m_Timer);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    GetStreamTimerContext(m_Timer)->Stream = this;
    m_State = AcxStreamStatePause;
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
NTSTATUS CCableStream::ReleaseHardware()
{
    PAGED_CODE();

    if (m_State == AcxStreamStateStop)
    {
        return STATUS_SUCCESS;
    }

    if (m_Timer)
    {
        WdfTimerStop(m_Timer, TRUE);
        WdfObjectDelete(m_Timer);
        m_Timer = NULL;
    }

    m_StartPosition = 0;
    m_Processed = 0;
    m_CurrentPacket = 0;
    m_State = AcxStreamStateStop;
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
NTSTATUS CCableStream::Run()
{
    PAGED_CODE();

    if (m_State == AcxStreamStateRun)
    {
        return STATUS_SUCCESS;
    }

    if (m_State != AcxStreamStatePause || m_PacketCount == 0)
    {
        return STATUS_INVALID_STATE_TRANSITION;
    }

    if (!m_Render)
    {
        m_Cable->SetCaptureActive(TRUE);
    }

    StartRunning();
    WdfTimerStart(m_Timer, WDF_REL_TIMEOUT_IN_MS(TIMER_PERIOD_MS));
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
NTSTATUS CCableStream::Pause()
{
    PAGED_CODE();

    if (m_State == AcxStreamStatePause)
    {
        return STATUS_SUCCESS;
    }

    if (m_State != AcxStreamStateRun)
    {
        return STATUS_INVALID_STATE_TRANSITION;
    }

    WdfTimerStop(m_Timer, TRUE);
    StopRunning();

    if (!m_Render)
    {
        m_Cable->SetCaptureActive(FALSE);
    }

    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
NTSTATUS CCableStream::AssignDrmContentId(_In_ PACXDRMRIGHTS DrmRights)
{
    PAGED_CODE();

    // copy-protected content doesn't go into the cable
    InterlockedExchange(&m_CopyProtect, DrmRights->CopyProtect ? 1 : 0);
    return STATUS_SUCCESS;
}

#pragma code_seg()
// The data up to the reported position has been moved: a capture client reads it, a render client may overwrite it.
NTSTATUS CCableStream::GetPresentationPosition(_Out_ PULONGLONG PositionInBlocks, _Out_ PULONGLONG QpcPosition)
{
    KIRQL irql;
    KeAcquireSpinLock(&m_Lock, &irql);

    LONGLONG qpc = KeQueryPerformanceCounter(NULL).QuadPart;
    if (m_State == AcxStreamStateRun)
    {
        Advance(qpc);
    }

    *PositionInBlocks = m_Processed / m_BlockAlign;
    *QpcPosition = (ULONGLONG)qpc;

    KeReleaseSpinLock(&m_Lock, irql);
    return STATUS_SUCCESS;
}

#pragma code_seg()
VOID CCableStream::StartRunning()
{
    KIRQL irql;
    KeAcquireSpinLock(&m_Lock, &irql);

    // the position continues from where the stream paused
    m_StartQpc = KeQueryPerformanceCounter(NULL).QuadPart;
    m_StartPosition = m_Processed;
    m_CurrentPacketStart.QuadPart = m_StartQpc;
    m_State = AcxStreamStateRun;

    KeReleaseSpinLock(&m_Lock, irql);
}

#pragma code_seg()
VOID CCableStream::StopRunning()
{
    KIRQL irql;
    KeAcquireSpinLock(&m_Lock, &irql);
    m_State = AcxStreamStatePause;
    KeReleaseSpinLock(&m_Lock, irql);
}

#pragma code_seg("PAGE")
NTSTATUS CCableStream::GetCurrentPacket(_Out_ PULONG CurrentPacket)
{
    PAGED_CODE();

    *CurrentPacket = (ULONG)ReadAcquire((volatile LONG*)&m_CurrentPacket);
    return STATUS_SUCCESS;
}

#pragma code_seg()
NTSTATUS CCableStream::GetCapturePacket(_Out_ PULONG LastCapturePacket, _Out_ PULONGLONG QpcPacketStart, _Out_ PBOOLEAN MoreData)
{
    // Advance changes both under the lock
    KIRQL irql;
    KeAcquireSpinLock(&m_Lock, &irql);
    *LastCapturePacket = m_CurrentPacket - 1;
    *QpcPacketStart = (ULONGLONG)m_LastPacketStart.QuadPart;
    KeReleaseSpinLock(&m_Lock, irql);

    *MoreData = FALSE;
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
NTSTATUS CCableStream::SetRenderPacket(_In_ ULONG Packet)
{
    PAGED_CODE();

    ULONG currentPacket = (ULONG)ReadAcquire((volatile LONG*)&m_CurrentPacket);
    if (Packet <= currentPacket)
    {
        return STATUS_DATA_LATE_ERROR;
    }

    if (Packet > currentPacket + 1)
    {
        return STATUS_DATA_OVERRUN;
    }

    return STATUS_SUCCESS;
}

#pragma code_seg()
VOID CCableStream::EvtTimer(_In_ WDFTIMER Timer)
{
    CCableStream* stream = GetStreamTimerContext(Timer)->Stream;

    KIRQL irql;
    KeAcquireSpinLock(&stream->m_Lock, &irql);
    if (stream->m_State == AcxStreamStateRun)
    {
        stream->Advance(KeQueryPerformanceCounter(NULL).QuadPart);
    }

    KeReleaseSpinLock(&stream->m_Lock, irql);
}

#pragma code_seg()
// The position in bytes (whole blocks) at a QPC time while the stream runs.
ULONGLONG CCableStream::PositionAt(_In_ LONGLONG Qpc)
{
    LONGLONG ticks = Qpc - m_StartQpc;
    if (ticks <= 0)
    {
        return m_StartPosition;
    }

    // split, so the multiplication doesn't overflow in long streams
    ULONGLONG frequency = (ULONGLONG)m_QpcFrequency.QuadPart;
    ULONGLONG seconds = (ULONGLONG)ticks / frequency;
    ULONGLONG remainder = (ULONGLONG)ticks % frequency;
    ULONGLONG bytes = seconds * m_BytesPerSecond + remainder * m_BytesPerSecond / frequency;
    return m_StartPosition + bytes - bytes % m_BlockAlign;
}

#pragma code_seg()
// Moves the data up to the current position and completes the packets the position passed.
VOID CCableStream::Advance(_In_ LONGLONG Qpc)
{
    ULONGLONG position = PositionAt(Qpc);
    ULONGLONG bufferSize = (ULONGLONG)m_PacketCount * m_PacketSize;

    // more than the whole buffer behind (e.g. a debugger break): the older data is gone
    if (position - m_Processed > bufferSize)
    {
        m_Processed = position - bufferSize;
    }

    Process(m_Processed, position);
    m_Processed = position;

    // packet numbers are 32-bit, like ACX's; the difference stays correct when they wrap
    ULONG passed = (ULONG)(position / m_PacketSize);
    while ((LONG)(passed - m_CurrentPacket) > 0)
    {
        InterlockedExchange64(&m_LastPacketStart.QuadPart, m_CurrentPacketStart.QuadPart);
        InterlockedExchange64(&m_CurrentPacketStart.QuadPart, Qpc);
        ULONG completedPacket = (ULONG)InterlockedIncrement((volatile LONG*)&m_CurrentPacket) - 1;
        (VOID)AcxRtStreamNotifyPacketComplete(m_Stream, completedPacket, (ULONGLONG)Qpc);
    }
}

#pragma code_seg()
// Moves the bytes From to To (linear positions; both whole blocks) between the packets and the cable.
VOID CCableStream::Process(_In_ ULONGLONG From, _In_ ULONGLONG To)
{
    ULONG samplesPerMs = CABLE_SAMPLE_RATE / 1000 * m_Channels;
    ULONGLONG bufferSize = (ULONGLONG)m_PacketCount * m_PacketSize;

    while (From < To)
    {
        ULONG offset = (ULONG)(From % bufferSize);
        ULONG index = offset / m_PacketSize;
        ULONG inPacket = offset % m_PacketSize;
        ULONG count = (ULONG)min((ULONGLONG)(m_PacketSize - inPacket), To - From);
        PBYTE data = (PBYTE)m_Packets[index] + (index == 0 ? m_FirstPacketOffset : 0) + inPacket;

        if (m_Render)
        {
            if (ReadAcquire(&m_CopyProtect) == 0)
            {
                m_Cable->Write(data, count, m_BytesPerSample, m_Channels);
            }
        }
        else
        {
            m_Cable->Read(
                data,
                count,
                m_BytesPerSample,
                CABLE_TARGET_LATENCY_MS * samplesPerMs,
                CABLE_MAX_LATENCY_MS * samplesPerMs,
                m_Channels);
        }

        From += count;
    }
}

//
// ACX callbacks
//

static CCableStream* GetCableStream(_In_ ACXSTREAM Stream)
{
    return GetStreamContext(Stream)->Stream;
}

#pragma code_seg("PAGE")
static VOID EvtStreamCleanup(_In_ WDFOBJECT Object)
{
    PAGED_CODE();

    PSTREAM_CONTEXT context = GetStreamContext((ACXSTREAM)Object);
    if (context->Circuit)
    {
        InterlockedDecrement(&context->Circuit->StreamCount);
        context->Circuit = NULL;
    }
}

#pragma code_seg("PAGE")
static VOID EvtStreamDestroy(_In_ WDFOBJECT Object)
{
    PAGED_CODE();

    PSTREAM_CONTEXT context = GetStreamContext((ACXSTREAM)Object);
    delete context->Stream;
    context->Stream = NULL;
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamPrepareHardware(_In_ ACXSTREAM Stream)
{
    PAGED_CODE();
    return GetCableStream(Stream)->PrepareHardware();
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamReleaseHardware(_In_ ACXSTREAM Stream)
{
    PAGED_CODE();
    return GetCableStream(Stream)->ReleaseHardware();
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamRun(_In_ ACXSTREAM Stream)
{
    PAGED_CODE();
    return GetCableStream(Stream)->Run();
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamPause(_In_ ACXSTREAM Stream)
{
    PAGED_CODE();
    return GetCableStream(Stream)->Pause();
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamAssignDrmContentId(_In_ ACXSTREAM Stream, _In_ ULONG ContentId, _In_ PACXDRMRIGHTS DrmRights)
{
    PAGED_CODE();
    UNREFERENCED_PARAMETER(ContentId);
    return GetCableStream(Stream)->AssignDrmContentId(DrmRights);
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamGetHwLatency(_In_ ACXSTREAM Stream, _Out_ ULONG* FifoSize, _Out_ ULONG* Delay)
{
    PAGED_CODE();
    UNREFERENCED_PARAMETER(Stream);

    *FifoSize = 0;
    *Delay = 0;
    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamAllocateRtPackets(
    _In_ ACXSTREAM Stream,
    _In_ ULONG PacketCount,
    _In_ ULONG PacketSize,
    _Out_ PACX_RTPACKET* Packets)
{
    PAGED_CODE();
    return GetCableStream(Stream)->AllocateRtPackets(PacketCount, PacketSize, Packets);
}

#pragma code_seg("PAGE")
static VOID EvtStreamFreeRtPackets(_In_ ACXSTREAM Stream, _In_ PACX_RTPACKET Packets, _In_ ULONG PacketCount)
{
    PAGED_CODE();
    GetCableStream(Stream)->FreeRtPackets(Packets, PacketCount);
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamSetRenderPacket(_In_ ACXSTREAM Stream, _In_ ULONG Packet, _In_ ULONG Flags, _In_ ULONG EosPacketLength)
{
    PAGED_CODE();
    UNREFERENCED_PARAMETER(EosPacketLength);

    // a shortened last packet isn't supported
    if (Flags & KSSTREAM_HEADER_OPTIONSF_ENDOFSTREAM)
    {
        return STATUS_INVALID_PARAMETER;
    }

    return GetCableStream(Stream)->SetRenderPacket(Packet);
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamGetCurrentPacket(_In_ ACXSTREAM Stream, _Out_ PULONG CurrentPacket)
{
    PAGED_CODE();
    return GetCableStream(Stream)->GetCurrentPacket(CurrentPacket);
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamGetCapturePacket(
    _In_ ACXSTREAM Stream,
    _Out_ PULONG LastCapturePacket,
    _Out_ PULONGLONG QpcPacketStart,
    _Out_ PBOOLEAN MoreData)
{
    PAGED_CODE();
    return GetCableStream(Stream)->GetCapturePacket(LastCapturePacket, QpcPacketStart, MoreData);
}

#pragma code_seg("PAGE")
static NTSTATUS EvtStreamGetPresentationPosition(_In_ ACXSTREAM Stream, _Out_ PULONGLONG PositionInBlocks, _Out_ PULONGLONG QpcPosition)
{
    PAGED_CODE();
    return GetCableStream(Stream)->GetPresentationPosition(PositionInBlocks, QpcPosition);
}

#pragma code_seg("PAGE")
NTSTATUS Cable_EvtCreateStream(
    _In_ WDFDEVICE Device,
    _In_ ACXCIRCUIT Circuit,
    _In_ ACXPIN Pin,
    _In_ PACXSTREAM_INIT StreamInit,
    _In_ ACXDATAFORMAT StreamFormat,
    _In_ const GUID* SignalProcessingMode,
    _In_ ACXOBJECTBAG VarArguments)
{
    PAGED_CODE();
    UNREFERENCED_PARAMETER(Pin);
    UNREFERENCED_PARAMETER(SignalProcessingMode);
    UNREFERENCED_PARAMETER(VarArguments);

    PCIRCUIT_CONTEXT circuitContext = GetCircuitContext(Circuit);

    ACX_STREAM_CALLBACKS streamCallbacks;
    ACX_STREAM_CALLBACKS_INIT(&streamCallbacks);
    streamCallbacks.EvtAcxStreamPrepareHardware = EvtStreamPrepareHardware;
    streamCallbacks.EvtAcxStreamReleaseHardware = EvtStreamReleaseHardware;
    streamCallbacks.EvtAcxStreamRun = EvtStreamRun;
    streamCallbacks.EvtAcxStreamPause = EvtStreamPause;
    streamCallbacks.EvtAcxStreamAssignDrmContentId = EvtStreamAssignDrmContentId;

    NTSTATUS status = AcxStreamInitAssignAcxStreamCallbacks(StreamInit, &streamCallbacks);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    ACX_RT_STREAM_CALLBACKS rtCallbacks;
    ACX_RT_STREAM_CALLBACKS_INIT(&rtCallbacks);
    rtCallbacks.EvtAcxStreamGetHwLatency = EvtStreamGetHwLatency;
    rtCallbacks.EvtAcxStreamAllocateRtPackets = EvtStreamAllocateRtPackets;
    rtCallbacks.EvtAcxStreamFreeRtPackets = EvtStreamFreeRtPackets;
    rtCallbacks.EvtAcxStreamGetCurrentPacket = EvtStreamGetCurrentPacket;
    rtCallbacks.EvtAcxStreamGetPresentationPosition = EvtStreamGetPresentationPosition;
    if (circuitContext->Render)
    {
        rtCallbacks.EvtAcxStreamSetRenderPacket = EvtStreamSetRenderPacket;
    }
    else
    {
        rtCallbacks.EvtAcxStreamGetCapturePacket = EvtStreamGetCapturePacket;
    }

    status = AcxStreamInitAssignAcxRtStreamCallbacks(StreamInit, &rtCallbacks);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    AcxStreamInitSetAcxRtStreamSupportsNotifications(StreamInit);

    // one stream per side, as on a device without a hardware mixer
    if (InterlockedIncrement(&circuitContext->StreamCount) > 1)
    {
        InterlockedDecrement(&circuitContext->StreamCount);
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    WDF_OBJECT_ATTRIBUTES attributes;
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attributes, STREAM_CONTEXT);
    attributes.EvtCleanupCallback = EvtStreamCleanup;
    attributes.EvtDestroyCallback = EvtStreamDestroy;

    ACXSTREAM stream;
    status = AcxRtStreamCreate(Device, Circuit, &attributes, &StreamInit, &stream);
    if (!NT_SUCCESS(status))
    {
        InterlockedDecrement(&circuitContext->StreamCount);
        return status;
    }

    // from here on EvtStreamCleanup releases the stream's slot
    GetStreamContext(stream)->Circuit = circuitContext;

    CCableStream* cableStream =
        new (POOL_FLAG_NON_PAGED, CABLE_POOLTAG_DRIVER) CCableStream(stream, StreamFormat, circuitContext->Cable, circuitContext->Render);
    if (!cableStream)
    {
        // the stream is deleted with its creation request
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    GetStreamContext(stream)->Stream = cableStream;
    return STATUS_SUCCESS;
}
