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
