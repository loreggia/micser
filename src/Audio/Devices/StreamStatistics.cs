namespace Micser.Audio.Devices;

/// <param name="Fill">Smoothed ring buffer fill level in device frames.</param>
/// <param name="TargetFill">Target fill level in device frames; it adapts to dropouts.</param>
/// <param name="Correction">Current resampling correction factor.</param>
/// <param name="Underruns">Blocks that couldn't be served completely because the ring buffer ran empty.</param>
/// <param name="Overruns">Writes that were dropped because the ring buffer was full.</param>
/// <param name="TargetMilliseconds">The target fill level as latency.</param>
public readonly record struct StreamStatistics(double Fill, double TargetFill, double Correction, long Underruns, long Overruns, double TargetMilliseconds);
