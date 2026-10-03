using System.Text.Json.Serialization;

namespace Micser.DriverUtility;

/// <summary>
/// What "status" prints as JSON; the shell reads it.
/// </summary>
/// <param name="Installed">Whether the device is present.</param>
/// <param name="Problem">The device's problem code, or null when it runs.</param>
/// <param name="InstalledVersion">The installed driver's version.</param>
/// <param name="BundledVersion">The version of the driver package next to the utility.</param>
/// <param name="CableCount">The configured number of cables.</param>
/// <param name="UpdateAvailable">Whether the bundled driver is newer than the installed one.</param>
/// <param name="Cables">The cables, in order.</param>
internal sealed record DriverStatus(
    bool Installed,
    uint? Problem,
    string? InstalledVersion,
    string? BundledVersion,
    int CableCount,
    bool UpdateAvailable,
    IReadOnlyList<CableStatus> Cables);

/// <param name="Layout">"stereo", "5.1" or "7.1".</param>
/// <param name="FormatsMatch">Whether both endpoints of the cable exist and have its layout's format; "sync-formats" fixes them.</param>
internal sealed record CableStatus(string Layout, bool FormatsMatch);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DriverStatus))]
internal sealed partial class DriverStatusJsonContext : JsonSerializerContext;
