using System.ComponentModel.DataAnnotations;
using Micser.Audio;
using Micser.Audio.Devices;
using Micser.Engine.Plugins;

namespace Micser.Engine.Contracts;

/// <param name="SourceChannel">The only channel of the source port taken (0-based), or null for all.</param>
/// <param name="TargetChannel">The only channel of the target port added to (0-based), or null for all.</param>
public sealed record ConnectionDto(
    Guid Id,
    Guid SourceModuleId,
    string SourcePort,
    Guid TargetModuleId,
    string TargetPort,
    int? SourceChannel = null,
    int? TargetChannel = null
);

/// <param name="SourceChannel">
/// The only channel of the source port to take (0-based), or null for all. A channel the port doesn't have right now is silent.
/// </param>
/// <param name="TargetChannel">
/// The only channel of the target port to add to (0-based), or null for all. With a fixed <see cref="ModuleDto.ChannelCount"/>, it must be
/// below it.
/// </param>
public sealed record CreateConnectionRequest(
    Guid SourceModuleId,
    [Required] string SourcePort,
    Guid TargetModuleId,
    [Required] string TargetPort,
    [Range(0, AudioModule.MaxChannelCount - 1)] int? SourceChannel = null,
    [Range(0, AudioModule.MaxChannelCount - 1)] int? TargetChannel = null
);

/// <param name="SampleRate">The sample rate the graph is processed at, in Hz.</param>
/// <param name="FrameCount">Frames per processing block; smaller blocks lower the latency and raise the CPU load.</param>
public sealed record EngineSettingsDto(
    [Range(8000, 192000)] int SampleRate = 48000,
    [Range(32, 4800)] int FrameCount = 240
);

/// <param name="Blocks">Blocks processed since the engine started.</param>
/// <param name="LateBlocks">Times processing fell behind and skipped ahead.</param>
/// <param name="MaxProcessingMilliseconds">Longest time a block took to process since the engine started.</param>
public sealed record EngineStatusDto(
    bool IsRunning,
    EngineSettingsDto Settings,
    long Blocks,
    long LateBlocks,
    double MaxProcessingMilliseconds
);

/// <summary>
/// The channel layouts of a module's ports while the audio runs.
/// </summary>
/// <param name="Inputs">The layouts of the input ports, by name.</param>
/// <param name="Outputs">The layouts of the output ports, by name.</param>
public sealed record ModulePortLayoutsDto(
    Guid ModuleId,
    IReadOnlyDictionary<string, PortLayoutDto> Inputs,
    IReadOnlyDictionary<string, PortLayoutDto> Outputs
);

/// <summary>
/// The channel layout of a port in the last processed block.
/// </summary>
/// <param name="ChannelCount">0 while the port carries nothing, e.g. a device output without an open device or a module that isn't processed.</param>
/// <param name="Speakers">
/// The speaker of each channel, or null if the channels have no speaker positions or one that <see cref="SpeakerPosition"/> doesn't name
/// (the reserved bits of the mask).
/// </param>
public sealed record PortLayoutDto(int ChannelCount, IReadOnlyList<SpeakerPosition>? Speakers)
{
    /// <summary>
    /// The speakers that <see cref="SpeakerPosition"/> names.
    /// </summary>
    private const uint KnownSpeakers = 0x3FFFF;

    public static PortLayoutDto From(ChannelLayout layout)
    {
        var hasKnownSpeakers = layout.HasSpeakerPositions && ((uint)layout.Speakers & ~KnownSpeakers) == 0;
        return new PortLayoutDto(
            layout.ChannelCount,
            hasKnownSpeakers
                ? [.. Enumerable.Range(0, layout.ChannelCount).Select(c => (SpeakerPosition)(uint)layout.GetSpeaker(c))]
                : null
        );
    }
}

/// <summary>
/// The speaker of a channel (WAVEFORMATEXTENSIBLE).
/// </summary>
public enum SpeakerPosition : uint
{
    FrontLeft = 0x1,
    FrontRight = 0x2,
    FrontCenter = 0x4,
    LowFrequency = 0x8,
    BackLeft = 0x10,
    BackRight = 0x20,
    FrontLeftOfCenter = 0x40,
    FrontRightOfCenter = 0x80,
    BackCenter = 0x100,
    SideLeft = 0x200,
    SideRight = 0x400,
    TopCenter = 0x800,
    TopFrontLeft = 0x1000,
    TopFrontCenter = 0x2000,
    TopFrontRight = 0x4000,
    TopBackLeft = 0x8000,
    TopBackCenter = 0x10000,
    TopBackRight = 0x20000,
}

/// <summary>
/// The levels of one port of a module, per channel as linear amplitude (1 = full scale). Pushed by the hub, not part of the HTTP API.
/// </summary>
/// <param name="Port">The output port, or null for the signal a module without outputs passes on (e.g. what a device output plays).</param>
/// <param name="Peak">The highest absolute sample value since the previous push.</param>
/// <param name="Rms">The RMS level, smoothed over about 300 ms.</param>
public sealed record PortLevelsDto(string? Port, float[] Peak, float[] Rms);

/// <summary>
/// Preferences of the web UI. Kept in the engine's configuration, because the UI's origin (the engine's random port) changes on every start,
/// so browser storage wouldn't keep them.
/// </summary>
/// <param name="ShowStreamStatistics">Whether device widgets show dropouts and the buffer size.</param>
/// <param name="SnapToGrid">Whether modules snap to the grid when moved.</param>
/// <param name="Language">The UI's language, e.g. "de"; null follows the system. Languages the UI doesn't have also follow the system.</param>
/// <param name="ShowChannelsByDefault">Whether modules the UI adds show their channel connectors.</param>
/// <param name="Theme">The UI's color theme; null follows the system's light or dark mode.</param>
/// <param name="ShowHelp">Whether info buttons explain modules and their parameters.</param>
public sealed record UiPreferencesDto(
    bool ShowStreamStatistics = false,
    bool SnapToGrid = true,
    string? Language = null,
    bool ShowChannelsByDefault = false,
    UiThemeDto? Theme = null,
    bool ShowHelp = true
);

/// <summary>
/// A color theme of the web UI.
/// </summary>
/// <param name="Name">The theme's name, e.g. "HighContrast". The UI shows a theme it doesn't have in its plain light or dark theme.</param>
/// <param name="ColorMode">Whether the theme is light or dark, for what isn't drawn by the UI, e.g. the shell's window.</param>
public sealed record UiThemeDto(string Name, UiColorMode ColorMode);

/// <summary>
/// Whether a theme is light or dark.
/// </summary>
public enum UiColorMode
{
    Light,
    Dark,
}

/// <summary>
/// The channel layout of a virtual audio cable; both of its sides use it.
/// </summary>
public enum CableLayout
{
    Stereo,

    /// <summary>
    /// 5.1 with side speakers.
    /// </summary>
    Surround51,

    /// <summary>
    /// 7.1 with back and side speakers.
    /// </summary>
    Surround71,
}

/// <summary>
/// A virtual audio cable: an output device that apps play to ("Micser Cable N Input"), whose sound comes out of an input device that
/// apps record from ("Micser Cable N Output").
/// </summary>
public sealed record CableDto([EnumDataType(typeof(CableLayout))] CableLayout Layout);

/// <summary>
/// The virtual audio cables the engine creates.
/// </summary>
/// <param name="IsSupported">Whether the engine creates cables on this platform (Linux with PipeWire). On Windows a driver provides
/// them, which the shell installs; the engine then keeps <paramref name="Cables"/> but doesn't apply them.</param>
/// <param name="Cables">The cables in order, cable 1 first.</param>
public sealed record VirtualCablesDto(bool IsSupported, IReadOnlyList<CableDto> Cables);

/// <param name="Cables">The cables in order, cable 1 first; none removes all.</param>
public sealed record UpdateCablesRequest([MaxLength(VirtualCables.MaxCount)] IReadOnlyList<CableDto> Cables);

/// <summary>
/// A plugin: loaded at the engine's start, failed to load, or staged for installation.
/// </summary>
/// <param name="Name">Null if the plugin's manifest can't be read.</param>
/// <param name="IsBuiltIn">Whether the plugin ships with the engine; built-in plugins can't be removed.</param>
/// <param name="Error">Why the plugin isn't loaded; null if it's loaded or only staged.</param>
/// <param name="WebUrl">The URL of the plugin's widget bundle (an ES module), if it's loaded and has widgets.</param>
/// <param name="PendingChange">A change that's applied when the engine restarts.</param>
public sealed record PluginDto(
    string Id,
    string? Name,
    string? Version,
    bool IsBuiltIn,
    bool IsLoaded,
    string? Error,
    string? WebUrl,
    PluginChange PendingChange
);
