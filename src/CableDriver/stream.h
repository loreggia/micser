/*++

Module Name:

    stream.h

Abstract:

    RT packet stream of one side of a cable. The stream's position follows the QPC clock. A 1 ms timer, and every position
    request, moves the data between the packets and the cable up to the position and completes each packet when the position
    passes its end, so clients that poll the position (exclusive mode without events) and clients that wait for packets both see
    consistent data.

--*/

#ifndef _MICSERCABLE_STREAM_H_
#define _MICSERCABLE_STREAM_H_

#define MAX_PACKET_COUNT    2
#define HNS_PER_SEC         10000000

class CCableStream
{
public:
    CCableStream(_In_ ACXSTREAM Stream, _In_ ACXDATAFORMAT Format, _In_ CCable* Cable, _In_ BOOLEAN Render);

    NTSTATUS AllocateRtPackets(_In_ ULONG PacketCount, _In_ ULONG PacketSize, _Out_ PACX_RTPACKET* Packets);
    VOID FreeRtPackets(_In_ PACX_RTPACKET Packets, _In_ ULONG PacketCount);
    NTSTATUS PrepareHardware();
    NTSTATUS ReleaseHardware();
    NTSTATUS Run();
    NTSTATUS Pause();
    NTSTATUS AssignDrmContentId(_In_ PACXDRMRIGHTS DrmRights);
    NTSTATUS GetPresentationPosition(_Out_ PULONGLONG PositionInBlocks, _Out_ PULONGLONG QpcPosition);
    NTSTATUS GetCurrentPacket(_Out_ PULONG CurrentPacket);
    NTSTATUS GetCapturePacket(_Out_ PULONG LastCapturePacket, _Out_ PULONGLONG QpcPacketStart, _Out_ PBOOLEAN MoreData);
    NTSTATUS SetRenderPacket(_In_ ULONG Packet);

    static EVT_WDF_TIMER EvtTimer;

private:
    VOID StartRunning();
    VOID StopRunning();
    ULONGLONG PositionAt(_In_ LONGLONG Qpc);
    VOID Advance(_In_ LONGLONG Qpc);
    VOID Process(_In_ ULONGLONG From, _In_ ULONGLONG To);

    ACXSTREAM           m_Stream;
    CCable*             m_Cable;
    BOOLEAN             m_Render;
    volatile LONG       m_CopyProtect;
    ULONG               m_BytesPerSecond;
    ULONG               m_BlockAlign;
    ULONG               m_BytesPerSample;
    ULONG               m_Channels;

    PVOID               m_Packets[MAX_PACKET_COUNT];
    ULONG               m_PacketCount;
    ULONG               m_PacketSize;
    ULONG               m_FirstPacketOffset;

    // The timer and position requests move the data, so a polling client never sees a position beyond the moved data. m_Lock
    // guards the fields below while the stream runs.
    WDFTIMER            m_Timer;
    KSPIN_LOCK          m_Lock;
    ACX_STREAM_STATE    m_State;
    LARGE_INTEGER       m_QpcFrequency;
    volatile ULONG      m_CurrentPacket;    // completed packets
    LARGE_INTEGER       m_CurrentPacketStart;
    LARGE_INTEGER       m_LastPacketStart;
    LONGLONG            m_StartQpc;         // of the last Run
    ULONGLONG           m_StartPosition;    // bytes, at the last Run
    ULONGLONG           m_Processed;        // bytes moved between the packets and the cable
};

EVT_ACX_CIRCUIT_CREATE_STREAM Cable_EvtCreateStream;

#endif // _MICSERCABLE_STREAM_H_
