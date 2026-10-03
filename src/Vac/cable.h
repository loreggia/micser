/*++

Module Name:

    cable.h

Abstract:

    Ring buffer that connects the render and capture stream of one cable.

--*/

#ifndef _MICSERVAC_CABLE_H_
#define _MICSERVAC_CABLE_H_

#define CABLE_BUFFER_SIZE           0x20000     // 128 KiB, about 340 ms of 48 kHz stereo 32-bit
#define CABLE_SURROUND_BUFFER_SIZE  0x80000     // 512 KiB, about 340 ms of 48 kHz 7.1 32-bit
#define CABLE_TARGET_LATENCY_MS     10
#define CABLE_MAX_LATENCY_MS        30

// Number of cables, read from the device's hardware key ("Device Parameters").
#define CABLE_COUNT_VALUE           L"CableCount"
#define MAX_CABLES                  16

// Channel count of each cable (2, 6 or 8; stereo otherwise), DWORD values "Cable1Channels", ... in the same key. Both sides of a cable
// use its layout, so the cable copies bytes. Windows keeps an endpoint's device format across device restarts, so DriverUtility sets
// the endpoints' format after changing a layout.
#define CABLE_CHANNELS_VALUE        L"Cable%uChannels"

// The channel mask of a cable's layout: stereo, 5.1 (side speakers, Windows' usual 5.1) or 7.1.
inline ULONG CableChannelMask(_In_ ULONG Channels)
{
    return Channels == 8 ? KSAUDIO_SPEAKER_7POINT1_SURROUND : Channels == 6 ? KSAUDIO_SPEAKER_5POINT1_SURROUND : KSAUDIO_SPEAKER_STEREO;
}

// Single producer (render stream) and single consumer (capture stream), lock-free.
// Both streams use the same format and advance on the same QPC clock, so the fill level only varies with timer jitter.
class CCable
{
public:
    CCable();
    ~CCable();

    // BufferSize must be a power of two.
    NTSTATUS Init(_In_ ULONG BufferSize);

    // Called by the capture stream when it starts or stops running. Starting drops stale data and waits for the target latency again.
    VOID SetCaptureActive(_In_ BOOLEAN Active);

    // Render side. Drops data while no capture stream runs or the buffer is full.
    VOID Write(_In_reads_bytes_(Count) const BYTE* Data, _In_ ULONG Count);

    // Capture side. Fills with silence until TargetLatency bytes are buffered and after an underrun. Skips the oldest data when more
    // than MaxLatency bytes are buffered.
    VOID Read
    (
        _Out_writes_bytes_(Count) BYTE* Data,
        _In_ ULONG Count,
        _In_ ULONG TargetLatency,
        _In_ ULONG MaxLatency,
        _In_ ULONG BlockAlign
    );

private:
    BYTE*           m_Buffer;
    ULONG           m_BufferSize;
    volatile LONG64 m_WritePosition;    // written by the producer only
    volatile LONG64 m_ReadPosition;     // written by the consumer only
    volatile LONG   m_CaptureActive;
    BOOLEAN         m_Primed;           // consumer only
};

typedef CCable* PCABLE;

#endif // _MICSERVAC_CABLE_H_
