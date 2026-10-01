using System.ComponentModel.DataAnnotations;

namespace Micser.Engine.Contracts;

public sealed record ConnectionDto(Guid Id, Guid SourceModuleId, string SourcePort, Guid TargetModuleId, string TargetPort);

public sealed record CreateConnectionRequest(
    Guid SourceModuleId,
    [Required] string SourcePort,
    Guid TargetModuleId,
    [Required] string TargetPort);

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
public sealed record UiPreferencesDto(bool ShowStreamStatistics = false, bool SnapToGrid = true);
