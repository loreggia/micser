/*++

Module Name:

    circuits.cpp

Abstract:

    The render and capture circuit of a cable: a host pin with the stream formats and a bridge pin with a jack that names the
    endpoint.

--*/

#include "driver.h"
#include "stream.h"

// Same for all cables; the device instance tells the cables apart.
static const GUID RENDER_COMPONENT_GUID = { 0x1e6e33b6, 0x4c5e, 0x4c59, { 0x9c, 0x9b, 0x7b, 0x2f, 0x3e, 0x8a, 0x5d, 0x10 } };
static const GUID CAPTURE_COMPONENT_GUID = { 0x1e6e33b6, 0x4c5e, 0x4c59, { 0x9c, 0x9b, 0x7b, 0x2f, 0x3e, 0x8a, 0x5d, 0x11 } };

// The reference strings of the circuits' interfaces; the INF's interface settings and DriverUtility (CableEndpoints) use them too.
DECLARE_CONST_UNICODE_STRING(RenderCircuitName, L"Render");
DECLARE_CONST_UNICODE_STRING(CaptureCircuitName, L"Capture");

enum
{
    HostPin = 0,
    BridgePin,
    PinCount
};

// The stream formats, in the cable's layout: 32-bit first (the device format, which Windows mixes into), then 24 valid bits in 32,
// packed 24 bits and 16 bits. Float isn't offered: Windows marks endpoints with a float device format "not present".
static const struct
{
    USHORT BitsPerSample;
    USHORT ValidBitsPerSample;
} CableFormats[] = { { 32, 32 }, { 32, 24 }, { 24, 24 }, { 16, 16 } };

#pragma code_seg("PAGE")
static VOID InitFormat(
    _Out_ KSDATAFORMAT_WAVEFORMATEXTENSIBLE* Format,
    _In_ ULONG Channels,
    _In_ USHORT BitsPerSample,
    _In_ USHORT ValidBitsPerSample)
{
    PAGED_CODE();

    RtlZeroMemory(Format, sizeof(*Format));
    Format->DataFormat.FormatSize = sizeof(KSDATAFORMAT_WAVEFORMATEXTENSIBLE);
    Format->DataFormat.MajorFormat = KSDATAFORMAT_TYPE_AUDIO;
    Format->DataFormat.SubFormat = KSDATAFORMAT_SUBTYPE_PCM;
    Format->DataFormat.Specifier = KSDATAFORMAT_SPECIFIER_WAVEFORMATEX;

    WAVEFORMATEXTENSIBLE* wave = &Format->WaveFormatExt;
    wave->Format.wFormatTag = WAVE_FORMAT_EXTENSIBLE;
    wave->Format.nChannels = (WORD)Channels;
    wave->Format.nSamplesPerSec = CABLE_SAMPLE_RATE;
    wave->Format.wBitsPerSample = BitsPerSample;
    wave->Format.nBlockAlign = (WORD)(Channels * BitsPerSample / 8);
    wave->Format.nAvgBytesPerSec = CABLE_SAMPLE_RATE * wave->Format.nBlockAlign;
    wave->Format.cbSize = sizeof(WAVEFORMATEXTENSIBLE) - sizeof(WAVEFORMATEX);
    wave->Samples.wValidBitsPerSample = ValidBitsPerSample;
    wave->dwChannelMask = CableChannelMask(Channels);
    wave->SubFormat = KSDATAFORMAT_SUBTYPE_PCM;
}

#pragma code_seg("PAGE")
static NTSTATUS AddFormats(_In_ WDFDEVICE Device, _In_ ACXCIRCUIT Circuit, _In_ ACXPIN Pin, _In_ ULONG Channels)
{
    PAGED_CODE();

    ACXDATAFORMATLIST formatList = AcxPinGetRawDataFormatList(Pin);
    if (!formatList)
    {
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    for (ULONG i = 0; i < ARRAYSIZE(CableFormats); i++)
    {
        KSDATAFORMAT_WAVEFORMATEXTENSIBLE format;
        InitFormat(&format, Channels, CableFormats[i].BitsPerSample, CableFormats[i].ValidBitsPerSample);

        ACX_DATAFORMAT_CONFIG formatConfig;
        ACX_DATAFORMAT_CONFIG_INIT_KS(&formatConfig, &format);

        WDF_OBJECT_ATTRIBUTES attributes;
        WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
        attributes.ParentObject = Circuit;

        ACXDATAFORMAT dataFormat;
        NTSTATUS status = AcxDataFormatCreate(Device, &attributes, &formatConfig, &dataFormat);
        if (NT_SUCCESS(status))
        {
            status = AcxDataFormatListAddDataFormat(formatList, dataFormat);
        }

        if (NT_SUCCESS(status) && i == 0)
        {
            status = AcxDataFormatListAssignDefaultDataFormat(formatList, dataFormat);
        }

        if (!NT_SUCCESS(status))
        {
            return status;
        }
    }

    return STATUS_SUCCESS;
}

#pragma code_seg("PAGE")
static NTSTATUS AddJack(_In_ ACXPIN Pin, _In_ BOOLEAN Render, _In_ ULONG Channels)
{
    PAGED_CODE();

    ACX_JACK_CONFIG jackConfig;
    ACX_JACK_CONFIG_INIT(&jackConfig);
    jackConfig.Description.ChannelMapping = Render ? CableChannelMask(Channels) : 0;
    jackConfig.Description.Color = Render ? 0xB3C98C : 0; // 0x00RRGGBB
    jackConfig.Description.ConnectionType = AcxConnTypeUnknown;
    jackConfig.Description.GeoLocation = AcxGeoLocFront;
    jackConfig.Description.GenLocation = AcxGenLocPrimaryBox;
    jackConfig.Description.PortConnection = AcxPortConnIntegratedDevice;

    WDF_OBJECT_ATTRIBUTES attributes;
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.ParentObject = Pin;

    ACXJACK jack;
    NTSTATUS status = AcxJackCreate(Pin, &attributes, &jackConfig, &jack);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    return AcxPinAddJacks(Pin, &jack, 1);
}

#pragma code_seg("PAGE")
static NTSTATUS EvtPinSetDataFormat(_In_ ACXPIN Pin, _In_ ACXDATAFORMAT DataFormat)
{
    PAGED_CODE();
    UNREFERENCED_PARAMETER(Pin);
    UNREFERENCED_PARAMETER(DataFormat);

    return STATUS_NOT_SUPPORTED;
}

#pragma code_seg("PAGE")
static NTSTATUS EvtBridgePinRetrieveName(_In_ ACXPIN Pin, _Out_ PUNICODE_STRING Name)
{
    PAGED_CODE();

    PCIRCUIT_CONTEXT context = GetCircuitContext(AcxPinGetCircuit(Pin));
    return RtlUnicodeStringPrintf(Name, L"Cable %u %s", context->Number, context->Render ? L"Input" : L"Output");
}

#pragma code_seg("PAGE")
NTSTATUS Cable_CreateCircuit(_In_ WDFDEVICE Device, _In_ BOOLEAN Render, _Out_ ACXCIRCUIT* Circuit)
{
    PAGED_CODE();

    *Circuit = NULL;
    PCABLE_DEVICE_CONTEXT deviceContext = GetCableDeviceContext(Device);

    PACXCIRCUIT_INIT circuitInit = AcxCircuitInitAllocate(Device);
    if (!circuitInit)
    {
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    AcxCircuitInitSetComponentId(circuitInit, Render ? &RENDER_COMPONENT_GUID : &CAPTURE_COMPONENT_GUID);
    AcxCircuitInitSetCircuitType(circuitInit, Render ? AcxCircuitTypeRender : AcxCircuitTypeCapture);

    NTSTATUS status = AcxCircuitInitAssignName(circuitInit, Render ? &RenderCircuitName : &CaptureCircuitName);
    if (NT_SUCCESS(status))
    {
        status = AcxCircuitInitAssignAcxCreateStreamCallback(circuitInit, Cable_EvtCreateStream);
    }

    if (!NT_SUCCESS(status))
    {
        AcxCircuitInitFree(circuitInit);
        return status;
    }

    WDF_OBJECT_ATTRIBUTES attributes;
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attributes, CIRCUIT_CONTEXT);

    ACXCIRCUIT circuit;
    status = AcxCircuitCreate(Device, &attributes, &circuitInit, &circuit);
    if (!NT_SUCCESS(status))
    {
        if (circuitInit)
        {
            AcxCircuitInitFree(circuitInit);
        }

        return status;
    }

    PCIRCUIT_CONTEXT circuitContext = GetCircuitContext(circuit);
    circuitContext->Number = deviceContext->Number;
    circuitContext->Render = Render;
    circuitContext->Cable = deviceContext->Cable;

    ACXPIN pins[PinCount];

    // Host pin: the streams. Render streams flow into the circuit, capture streams out of it.
    ACX_PIN_CALLBACKS hostCallbacks;
    ACX_PIN_CALLBACKS_INIT(&hostCallbacks);
    hostCallbacks.EvtAcxPinSetDataFormat = EvtPinSetDataFormat;

    ACX_PIN_CONFIG pinConfig;
    ACX_PIN_CONFIG_INIT(&pinConfig);
    pinConfig.Type = Render ? AcxPinTypeSink : AcxPinTypeSource;
    pinConfig.Communication = AcxPinCommunicationSink;
    pinConfig.Category = &KSCATEGORY_AUDIO;
    pinConfig.PinCallbacks = &hostCallbacks;

    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.ParentObject = circuit;
    status = AcxPinCreate(circuit, &attributes, &pinConfig, &pins[HostPin]);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    // Bridge pin: the endpoint. Windows ignores the pin name of a speaker, so both sides are line connectors.
    ACX_PIN_CALLBACKS bridgeCallbacks;
    ACX_PIN_CALLBACKS_INIT(&bridgeCallbacks);
    bridgeCallbacks.EvtAcxPinRetrieveName = EvtBridgePinRetrieveName;

    ACX_PIN_CONFIG_INIT(&pinConfig);
    pinConfig.Type = Render ? AcxPinTypeSource : AcxPinTypeSink;
    pinConfig.Communication = AcxPinCommunicationNone;
    pinConfig.Category = &KSNODETYPE_LINE_CONNECTOR;
    pinConfig.PinCallbacks = &bridgeCallbacks;

    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.ParentObject = circuit;
    status = AcxPinCreate(circuit, &attributes, &pinConfig, &pins[BridgePin]);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    status = AddJack(pins[BridgePin], Render, deviceContext->Channels);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    status = AddFormats(Device, circuit, pins[HostPin], deviceContext->Channels);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    status = AcxCircuitAddPins(circuit, pins, PinCount);
    if (!NT_SUCCESS(status))
    {
        return status;
    }

    *Circuit = circuit;
    return STATUS_SUCCESS;
}
