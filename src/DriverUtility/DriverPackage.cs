using System.Text.RegularExpressions;

namespace Micser.DriverUtility;

/// <summary>
/// A driver package (INF, SYS, CAT) in a folder, e.g. the one Velopack installs next to the utility.
/// </summary>
internal sealed partial class DriverPackage
{
    private DriverPackage(string directory, string infPath, Version version)
    {
        Directory = directory;
        InfPath = infPath;
        Version = version;
    }

    public string Directory { get; }

    public string InfPath { get; }

    public Version Version { get; }

    /// <summary>
    /// Loads the package from a folder; null when the folder has no INF with a DriverVer.
    /// </summary>
    public static DriverPackage? Load(string directory)
    {
        var infPath = System.IO.Directory.Exists(directory)
            ? System.IO.Directory.GetFiles(directory, "*.inf").FirstOrDefault()
            : null;
        if (infPath == null)
        {
            return null;
        }

        var version = ParseDriverVersion(File.ReadAllText(infPath));
        return version != null
            ? new DriverPackage(Path.GetFullPath(directory), Path.GetFullPath(infPath), version)
            : null;
    }

    /// <summary>
    /// Reads the version from an INF's "DriverVer = mm/dd/yyyy,w.x.y.z" line.
    /// </summary>
    public static Version? ParseDriverVersion(string infText)
    {
        var match = DriverVerRegex().Match(infText);
        return match.Success && Version.TryParse(match.Groups[1].Value, out var version) ? version : null;
    }

    [GeneratedRegex(@"^\s*DriverVer\s*=\s*[^,\r\n]*,\s*([0-9.]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex DriverVerRegex();
}
