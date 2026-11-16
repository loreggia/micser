using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;
using static Micser.DriverUtility.NativeMethods;

namespace Micser.DriverUtility;

/// <summary>
/// Installs, updates and removes the driver machine-wide. The utility and the driver package are copied to Program Files, which also
/// holds the "Apps and Features" entry's uninstaller, so the driver doesn't depend on the per-user app.
/// </summary>
internal static class DriverInstaller
{
    private const string DisplayName = "Micser Cable Driver";
    private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MicserCableDriver";

    public static string InstallDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Micser", "Driver");

    /// <summary>
    /// Installs the driver with a new device, or updates an existing one. Returns whether a reboot is needed.
    /// </summary>
    public static bool Install(DriverPackage package, int cableCount)
    {
        using (var existing = CableDriverDevice.Find())
        {
            if (existing is { IsPresent: true })
            {
                Log.Info("The device exists, updating it");
                var needReboot = Update(package);
                SetCableCount(cableCount, needReboot);
                return needReboot;
            }
        }

        // leftover devnodes from an earlier installation would get the driver too, and old cable devices would make Windows number the
        // new ones' endpoint names
        CableDriverDevice.RemoveAll();

        var installed = CopyToInstallDirectory(package);
        Log.Info($"Creating the device {CableDriverDevice.HardwareId} with {cableCount} cable(s)");
        using var device = CableDriverDevice.Create(installed.InfPath, cableCount);

        try
        {
            var needReboot = UpdateDriver(installed);
            WriteUninstallEntry(installed);
            return needReboot;
        }
        catch
        {
            CableDriverDevice.RemoveAll();
            throw;
        }
    }

    /// <summary>
    /// Removes the device, the driver packages and the installation. Returns whether a reboot is needed.
    /// </summary>
    public static bool Uninstall()
    {
        Log.Info("Removing the device");
        var needReboot = CableDriverDevice.RemoveAll();
        DeleteDriverPackages(keep: null);

        using (var key = Registry.LocalMachine.OpenSubKey(UninstallKeyPath, false))
        {
            if (key != null)
            {
                Registry.LocalMachine.DeleteSubKeyTree(UninstallKeyPath);
            }
        }

        RemoveInstallDirectory();
        return needReboot;
    }

    /// <summary>
    /// Installs a driver package on the existing device. Returns whether a reboot is needed.
    /// </summary>
    public static bool Update(DriverPackage package)
    {
        var installed = CopyToInstallDirectory(package);
        var needReboot = UpdateDriver(installed);
        WriteUninstallEntry(installed);
        return needReboot;
    }

    /// <summary>
    /// Sets the cable count of the running driver. After an update, the control device comes back once Windows restarted the device.
    /// </summary>
    private static void SetCableCount(int cableCount, bool needReboot)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            var result = CableControl.TryGet() is { } config
                ? CableControl.Set(
                    config with
                    {
                        CableCount = Math.Clamp(cableCount, 1, CableDriverDevice.MaxCableCount),
                    }
                )
                : CableSetResult.NotRunning;
            if (result != CableSetResult.NotRunning)
            {
                return;
            }

            if (needReboot)
            {
                Log.Warning("The driver runs after the reboot; the cable count stays as it is.");
                return;
            }

            if (stopwatch.Elapsed > TimeSpan.FromSeconds(10))
            {
                throw new InvalidOperationException(
                    "The driver doesn't run after the update; the cable count stays as it is."
                );
            }

            Thread.Sleep(250);
        }
    }

    private static DriverPackage CopyToInstallDirectory(DriverPackage package)
    {
        var target = InstallDirectory;
        if (string.Equals(package.Directory.TrimEnd('\\'), target, StringComparison.OrdinalIgnoreCase))
        {
            return package;
        }

        Log.Info($"Copying the driver package to {target}");
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(package.Directory))
        {
            var extension = Path.GetExtension(file);
            if (extension is ".inf" or ".sys" or ".cat")
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }
        }

        File.Copy(Environment.ProcessPath!, Path.Combine(target, Path.GetFileName(Environment.ProcessPath!)), true);
        return DriverPackage.Load(target)
            ?? throw new InvalidOperationException($"No driver package in {target} after copying.");
    }

    /// <summary>
    /// Deletes all Micser driver packages from the driver store, except the one in use.
    /// </summary>
    private static void DeleteDriverPackages(string? keep)
    {
        var infDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "INF");
        foreach (var inf in Directory.GetFiles(infDirectory, "oem*.inf"))
        {
            var name = Path.GetFileName(inf);
            if (
                string.Equals(name, keep, StringComparison.OrdinalIgnoreCase)
                || !File.ReadAllText(inf).Contains(CableDriverDevice.HardwareId, StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            Log.Info($"Deleting the driver package {name}");
            if (!SetupUninstallOEMInf(name, SUOI_FORCEDELETE, 0))
            {
                Log.Warning($"Deleting {name} failed: {new Win32Exception().Message}");
            }
        }
    }

    private static void RemoveInstallDirectory()
    {
        var directory = InstallDirectory;
        if (!Directory.Exists(directory))
        {
            return;
        }

        var running = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.Equals(running, directory, StringComparison.OrdinalIgnoreCase))
        {
            Directory.Delete(directory, true);
            return;
        }

        // the running utility can't delete its own folder; ping waits about 3 s (timeout needs console input)
        Process.Start(
            new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c ping -n 4 127.0.0.1 > nul & rmdir /s /q \"{directory}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Path.GetTempPath(),
            }
        );
    }

    private static bool UpdateDriver(DriverPackage package)
    {
        Log.Info($"Installing the driver {package.Version} from {package.InfPath}");

        // forced: every dev build has the same version
        if (
            !UpdateDriverForPlugAndPlayDevices(
                0,
                CableDriverDevice.HardwareId,
                package.InfPath,
                INSTALLFLAG_FORCE,
                out var needReboot
            )
        )
        {
            throw new Win32Exception(
                System.Runtime.InteropServices.Marshal.GetLastPInvokeError(),
                "Installing the driver failed"
            );
        }

        using var device = CableDriverDevice.Find();
        DeleteDriverPackages(keep: device?.GetDriverInfName());
        return needReboot;
    }

    private static void WriteUninstallEntry(DriverPackage package)
    {
        var utility = Path.Combine(package.Directory, Path.GetFileName(Environment.ProcessPath!));
        using var key = Registry.LocalMachine.CreateSubKey(UninstallKeyPath, true);
        key.SetValue("DisplayName", DisplayName);
        key.SetValue("DisplayVersion", package.Version.ToString());
        key.SetValue("Publisher", "Micser");
        key.SetValue("InstallLocation", package.Directory);
        key.SetValue("UninstallString", $"\"{utility}\" uninstall");
        key.SetValue("QuietUninstallString", $"\"{utility}\" uninstall");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }
}
