/*++

Module Name:

    cable.cpp

Abstract:

    Ring buffer that connects the render and capture stream of one cable.

--*/

#include "definitions.h"
#include "cable.h"

#define CABLE_POOLTAG 'BCVM'

#pragma code_seg()
// A stream sample as a 32-bit sample: 16- and 24-bit samples are the high bits.
static __forceinline LONG ToCableSample(_In_reads_bytes_(BytesPerSample) const BYTE* Data, _In_ ULONG BytesPerSample)
{
    switch (BytesPerSample)
    {
        case 2:
            return (LONG)(*(const SHORT UNALIGNED*)Data) * 0x10000;
        case 3:
            return (LONG)(((ULONG)Data[0] << 8) | ((ULONG)Data[1] << 16) | ((ULONG)Data[2] << 24));
        default:
            return *(const LONG UNALIGNED*)Data;
    }
}

#pragma code_seg()
// A 32-bit sample as a stream sample, rounded to nearest and clamped at full scale.
static __forceinline VOID FromCableSample(_In_ LONG Sample, _Out_writes_bytes_(BytesPerSample) BYTE* Data, _In_ ULONG BytesPerSample)
{
    switch (BytesPerSample)
    {
        case 2:
            *(SHORT UNALIGNED*)Data = (SHORT)(Sample >= 0x7FFF8000 ? 0x7FFF : (Sample + 0x8000) >> 16);
            break;
        case 3:
        {
            LONG value = Sample >= 0x7FFFFF80 ? 0x7FFFFF : (Sample + 0x80) >> 8;
            Data[0] = (BYTE)value;
            Data[1] = (BYTE)(value >> 8);
            Data[2] = (BYTE)(value >> 16);
            break;
        }
        default:
            *(LONG UNALIGNED*)Data = Sample;
            break;
    }
}

#pragma code_seg("PAGE")
CCable::CCable()
    : m_Samples(NULL),
      m_SampleCount(0),
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

    if (m_Samples)
    {
        ExFreePoolWithTag(m_Samples, CABLE_POOLTAG);
        m_Samples = NULL;
    }
}

#pragma code_seg("PAGE")
NTSTATUS CCable::Init(_In_ ULONG BufferSize)
{
    PAGED_CODE();

    if (BufferSize < sizeof(LONG) || (BufferSize & (BufferSize - 1)) != 0)
    {
        return STATUS_INVALID_PARAMETER;
    }

    m_Samples = (LONG*)ExAllocatePool2(POOL_FLAG_NON_PAGED, BufferSize, CABLE_POOLTAG);
    if (!m_Samples)
    {
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    m_SampleCount = BufferSize / sizeof(LONG);
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
VOID CCable::Write(_In_reads_bytes_(Count) const BYTE* Data, _In_ ULONG Count, _In_ ULONG BytesPerSample, _In_ ULONG Channels)
{
    NT_ASSERT(Count % (BytesPerSample * Channels) == 0);

    if (!m_Samples || ReadAcquire(&m_CaptureActive) == 0)
    {
        return;
    }

    LONG64 writePosition = m_WritePosition;
    LONG64 used = writePosition - ReadAcquire64(&m_ReadPosition);
    ULONG count = (ULONG)min((LONG64)(Count / BytesPerSample), (LONG64)m_SampleCount - used);
    count -= count % Channels;
    ULONG offset = (ULONG)(writePosition & (m_SampleCount - 1));

    if (BytesPerSample == sizeof(LONG))
    {
        ULONG first = min(count, m_SampleCount - offset);
        RtlCopyMemory(m_Samples + offset, Data, first * sizeof(LONG));
        RtlCopyMemory(m_Samples, Data + first * sizeof(LONG), (count - first) * sizeof(LONG));
    }
    else
    {
        for (ULONG i = 0; i < count; i++)
        {
            m_Samples[(offset + i) & (m_SampleCount - 1)] = ToCableSample(Data + i * BytesPerSample, BytesPerSample);
        }
    }

    WriteRelease64(&m_WritePosition, writePosition + count);
}

#pragma code_seg()
VOID CCable::Read
(
    _Out_writes_bytes_(Count) BYTE* Data,
    _In_ ULONG Count,
    _In_ ULONG BytesPerSample,
    _In_ ULONG TargetLatency,
    _In_ ULONG MaxLatency,
    _In_ ULONG Channels
)
{
    NT_ASSERT(Count % (BytesPerSample * Channels) == 0);

    ULONG samples = Count / BytesPerSample;
    ULONG count = 0;

    if (m_Samples)
    {
        LONG64 readPosition = m_ReadPosition;
        LONG64 available = ReadAcquire64(&m_WritePosition) - readPosition;

        if (!m_Primed && available >= (LONG64)samples + TargetLatency)
        {
            m_Primed = TRUE;
        }

        if (m_Primed)
        {
            if (available > (LONG64)samples + MaxLatency)
            {
                LONG64 skip = available - samples - TargetLatency;
                skip -= skip % Channels;
                readPosition += skip;
                available -= skip;
            }

            count = (ULONG)min((LONG64)samples, available);
            count -= count % Channels;

            ULONG offset = (ULONG)(readPosition & (m_SampleCount - 1));
            if (BytesPerSample == sizeof(LONG))
            {
                ULONG first = min(count, m_SampleCount - offset);
                RtlCopyMemory(Data, m_Samples + offset, first * sizeof(LONG));
                RtlCopyMemory(Data + first * sizeof(LONG), m_Samples, (count - first) * sizeof(LONG));
            }
            else
            {
                for (ULONG i = 0; i < count; i++)
                {
                    FromCableSample(m_Samples[(offset + i) & (m_SampleCount - 1)], Data + i * BytesPerSample, BytesPerSample);
                }
            }

            WriteRelease64(&m_ReadPosition, readPosition + count);

            if (count < samples)
            {
                // underrun: wait for the target latency again
                m_Primed = FALSE;
            }
        }
    }

    RtlZeroMemory(Data + count * BytesPerSample, Count - count * BytesPerSample);
}
