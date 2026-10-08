using System.ComponentModel.DataAnnotations;
using Micser.Audio;
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
    int? TargetChannel = null);

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
    [Range(0, AudioModule.MaxChannelCount - 1)] int? TargetChannel = null);

/// <param name="SampleRate">The sample rate the graph is processed at, in Hz.</param>
/// <param name="FrameCount">Frames per processing block; smaller blocks lower the latency and raise the CPU load.</param>
public sealed record EngineSettingsDto(
    [Range(8000, 192000)] int SampleRate = 48000,
    [Range(32, 4800)] int FrameCount = 240);

/// <param name="Blocks">Blocks processed since the engine started.</param>
/// <param name="LateBlocks">Times processing fell behind and skipped ahead.</param>
/// <param name="MaxProcessingMilliseconds">Longest time a block took to process since the engine started.</param>
public sealed record EngineStatusDto(bool IsRunning, EngineSettingsDto Settings, long Blocks, long LateBlocks, double MaxProcessingMilliseconds);

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
public sealed record UiPreferencesDto(bool ShowStreamStatistics = false, bool SnapToGrid = true, string? Language = null);

/// <summary>
/// A plugin: loaded at the engine's start, failed to load, or staged for installation.
/// </summary>
/// <param name="Name">Null if the plugin's manifest can't be read.</param>
/// <param name="IsBuiltIn">Whether the plugin ships with the engine; built-in plugins can't be removed.</param>
/// <param name="Error">Why the plugin isn't loaded; null if it's loaded or only staged.</param>
/// <param name="WebUrl">The URL of the plugin's widget bundle (an ES module), if it's loaded and has widgets.</param>
/// <param name="PendingChange">A change that's applied when the engine restarts.</param>
public sealed record PluginDto(string Id, string? Name, string? Version, bool IsBuiltIn, bool IsLoaded, string? Error, string? WebUrl, PluginChange PendingChange);
