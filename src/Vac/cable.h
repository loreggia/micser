/*++

Module Name:

    cable.h

Abstract:

    Ring buffer that connects the render and capture stream of one cable.

--*/

#ifndef _MICSERVAC_CABLE_H_
#define _MICSERVAC_CABLE_H_

#define CABLE_BUFFER_SIZE           0x20000     // 128 KiB, about 340 ms of 48 kHz stereo (the ring holds 32-bit samples)
#define CABLE_SURROUND_BUFFER_SIZE  0x80000     // 512 KiB, about 340 ms of 48 kHz 7.1
#define CABLE_TARGET_LATENCY_MS     10
#define CABLE_MAX_LATENCY_MS        30

// Number of cables, read from the device's hardware key ("Device Parameters").
#define CABLE_COUNT_VALUE           L"CableCount"
#define MAX_CABLES                  16

// Channel count of each cable (2, 6 or 8; stereo otherwise), DWORD values "Cable1Channels", ... in the same key. Both sides of a cable
// use its layout, so the cable never mixes channels. Windows keeps an endpoint's device format across device restarts, so DriverUtility sets
// the endpoints' format after changing a layout.
#define CABLE_CHANNELS_VALUE        L"Cable%uChannels"

// The channel mask of a cable's layout: stereo, 5.1 (side speakers, Windows' usual 5.1) or 7.1.
inline ULONG CableChannelMask(_In_ ULONG Channels)
{
    return Channels == 8 ? KSAUDIO_SPEAKER_7POINT1_SURROUND : Channels == 6 ? KSAUDIO_SPEAKER_5POINT1_SURROUND : KSAUDIO_SPEAKER_STEREO;
}

// Single producer (render stream) and single consumer (capture stream), lock-free.
// Both streams have the cable's channels and advance on the same QPC clock, so the fill level only varies with timer jitter. Their
// sample formats can differ (16-bit, packed 24-bit or 32-bit integer; 24 valid bits in 32 count as 32-bit): the ring holds 32-bit
// samples, and Write and Read convert. Positions and latencies count samples (frames times channels). Data is always whole frames.
class CCable
{
public:
    CCable();
    ~CCable();

    // BufferSize must be a power of two.
    NTSTATUS Init(_In_ ULONG BufferSize);

    // Called by the capture stream when it starts or stops running. Starting drops stale data and waits for the target latency again.
    VOID SetCaptureActive(_In_ BOOLEAN Active);

    // Render side. Drops data while no capture stream runs, and the frames that don't fit when the buffer is full.
    VOID Write(_In_reads_bytes_(Count) const BYTE* Data, _In_ ULONG Count, _In_ ULONG BytesPerSample, _In_ ULONG Channels);

    // Capture side. Fills with silence until TargetLatency samples are buffered and after an underrun. Skips the oldest whole frames
    // when more than MaxLatency samples are buffered. 32-bit samples are rounded to 16 or 24 bits, clamped at full scale.
    VOID Read
    (
        _Out_writes_bytes_(Count) BYTE* Data,
        _In_ ULONG Count,
        _In_ ULONG BytesPerSample,
        _In_ ULONG TargetLatency,
        _In_ ULONG MaxLatency,
        _In_ ULONG Channels
    );

private:
    LONG*           m_Samples;
    ULONG           m_SampleCount;      // a power of two
    volatile LONG64 m_WritePosition;    // written by the producer only
    volatile LONG64 m_ReadPosition;     // written by the consumer only
    volatile LONG   m_CaptureActive;
    BOOLEAN         m_Primed;           // consumer only
};

typedef CCable* PCABLE;

#endif // _MICSERVAC_CABLE_H_
