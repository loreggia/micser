/*++

Module Name:

    cable.cpp

Abstract:

    Ring buffer that connects the render and capture stream of one cable.

--*/

#include "definitions.h"
#include "cable.h"

#define CABLE_POOLTAG 'BCVM'

#pragma code_seg("PAGE")
CCable::CCable()
    : m_Buffer(NULL),
      m_BufferSize(0),
      m_WritePosition(0),
      m_ReadPosition(0),
      m_CaptureActive(0),
      m_Primed(FALSE)
{
    PAGED_CODE();
}

#pragma code_seg("PAGE")
CCable::~CCable()
{
    PAGED_CODE();

    if (m_Buffer)
    {
        ExFreePoolWithTag(m_Buffer, CABLE_POOLTAG);
        m_Buffer = NULL;
    }
}

#pragma code_seg("PAGE")
NTSTATUS CCable::Init(_In_ ULONG BufferSize)
{
    PAGED_CODE();

    if (BufferSize == 0 || (BufferSize & (BufferSize - 1)) != 0)
    {
        return STATUS_INVALID_PARAMETER;
    }

    m_Buffer = (BYTE*)ExAllocatePool2(POOL_FLAG_NON_PAGED, BufferSize, CABLE_POOLTAG);
    if (!m_Buffer)
    {
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    m_BufferSize = BufferSize;
    return STATUS_SUCCESS;
}

#pragma code_seg()
VOID CCable::SetCaptureActive(_In_ BOOLEAN Active)
{
    if (Active)
    {
        // the producer doesn't write while inactive, so everything up to its position is stale
        WriteRelease64(&m_ReadPosition, ReadAcquire64(&m_WritePosition));
        m_Primed = FALSE;
    }

    InterlockedExchange(&m_CaptureActive, Active ? 1 : 0);
}

#pragma code_seg()
VOID CCable::Write(_In_reads_bytes_(Count) const BYTE* Data, _In_ ULONG Count)
{
    if (!m_Buffer || ReadAcquire(&m_CaptureActive) == 0)
    {
        return;
    }

    LONG64 writePosition = m_WritePosition;
    LONG64 used = writePosition - ReadAcquire64(&m_ReadPosition);
    ULONG count = (ULONG)min((LONG64)Count, (LONG64)m_BufferSize - used);

    ULONG offset = (ULONG)(writePosition & (m_BufferSize - 1));
    ULONG first = min(count, m_BufferSize - offset);
    RtlCopyMemory(m_Buffer + offset, Data, first);
    RtlCopyMemory(m_Buffer, Data + first, count - first);

    WriteRelease64(&m_WritePosition, writePosition + count);
}

#pragma code_seg()
VOID CCable::Read
(
    _Out_writes_bytes_(Count) BYTE* Data,
    _In_ ULONG Count,
    _In_ ULONG TargetLatency,
    _In_ ULONG MaxLatency,
    _In_ ULONG BlockAlign
)
{
    ULONG count = 0;

    if (m_Buffer)
    {
        LONG64 readPosition = m_ReadPosition;
        LONG64 available = ReadAcquire64(&m_WritePosition) - readPosition;

        if (!m_Primed && available >= (LONG64)Count + TargetLatency)
        {
            m_Primed = TRUE;
        }

        if (m_Primed)
        {
            if (available > (LONG64)Count + MaxLatency)
            {
                LONG64 skip = available - Count - TargetLatency;
                skip -= skip % BlockAlign;
                readPosition += skip;
                available -= skip;
            }

            count = (ULONG)min((LONG64)Count, available);
            count -= count % BlockAlign;

            ULONG offset = (ULONG)(readPosition & (m_BufferSize - 1));
            ULONG first = min(count, m_BufferSize - offset);
            RtlCopyMemory(Data, m_Buffer + offset, first);
            RtlCopyMemory(Data + first, m_Buffer, count - first);

            WriteRelease64(&m_ReadPosition, readPosition + count);

            if (count < Count)
            {
                // underrun: wait for the target latency again
                m_Primed = FALSE;
            }
        }
    }

    RtlZeroMemory(Data + count, Count - count);
}
