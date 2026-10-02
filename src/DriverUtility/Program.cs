using System.Security.Principal;
using System.Text.Json;
using Micser.DriverUtility;

// Manages the virtual audio cable driver. The commands except "status" need administrator rights.
//
//   Micser.DriverUtility status                  prints the driver status as JSON (DriverStatus)
//   Micser.DriverUtility install [--count <n>]   installs the driver package next to the utility, or updates an existing device
//   Micser.DriverUtility update                  installs the driver package next to the utility on the existing device
//   Micser.DriverUtility set-count <n>           sets the number of cables (1-16) and restarts the device
//   Micser.DriverUtility uninstall               removes the device, the driver and the installation in Program Files
//
// Exit codes: 0 success, 3010 success but a reboot is needed, 1 failure, 2 invalid arguments, 4 nothing to work on (no device or
// package), 5 not elevated.

const int Success = 0;
const int Failure = 1;
const int InvalidArguments = 2;
const int NotFound = 4;
const int NotElevated = 5;
const int RebootRequired = 3010;

var command = args.FirstOrDefault()?.ToLowerInvariant();
if (command == null)
{
    Console.Error.WriteLine("Usage: Micser.DriverUtility status | install [--count <n>] | update | set-count <n> | uninstall");
    return InvalidArguments;
}

if (command == "status")
{
    Console.WriteLine(JsonSerializer.Serialize(GetStatus(), DriverStatusJsonContext.Default.DriverStatus));
    return Success;
}

if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
{
    Log.Error("This command needs administrator rights.");
    return NotElevated;
}

Log.Info($"Micser.DriverUtility {string.Join(' ', args)}");

try
{
    bool needReboot;
    switch (command)
    {
        case "install":
        {
            var countIndex = Array.IndexOf(args, "--count");
            var count = 1;
            if (countIndex >= 0 && (countIndex + 1 >= args.Length || !int.TryParse(args[countIndex + 1], out count)))
            {
                Log.Error("--count needs a number.");
                return InvalidArguments;
            }

            if (LoadBundledPackage() is not { } package)
            {
                return NotFound;
            }

            needReboot = DriverInstaller.Install(package, count);
            break;
        }

        case "update":
        {
            using (var device = VacDevice.Find())
            {
                if (device is not { IsPresent: true })
                {
                    Log.Error("The driver isn't installed.");
                    return NotFound;
                }
            }

            if (LoadBundledPackage() is not { } package)
            {
                return NotFound;
            }

            needReboot = DriverInstaller.Update(package);
            break;
        }

        case "set-count":
        {
            if (args.Length < 2 || !int.TryParse(args[1], out var count) || count < 1 || count > VacDevice.MaxCableCount)
            {
                Log.Error($"set-count needs a number from 1 to {VacDevice.MaxCableCount}.");
                return InvalidArguments;
            }

            using var device = VacDevice.Find();
            if (device is not { IsPresent: true })
            {
                Log.Error("The driver isn't installed.");
                return NotFound;
            }

            Log.Info($"Setting {count} cable(s)");
            device.SetCableCount(count);
            needReboot = device.Restart();
            break;
        }

        case "uninstall":
            needReboot = DriverInstaller.Uninstall();
            break;

        default:
            Log.Error($"Unknown command \"{command}\".");
            return InvalidArguments;
    }

    if (needReboot)
    {
        Log.Info("Done; Windows has to restart to finish.");
        return RebootRequired;
    }

    Log.Info("Done.");
    return Success;
}
catch (Exception ex)
{
    Log.Error(ex.ToString());
    return Failure;
}

static DriverStatus GetStatus()
{
    var bundled = DriverPackage.Load(AppContext.BaseDirectory)?.Version;
    using var device = VacDevice.Find();
    if (device is not { IsPresent: true })
    {
        return new DriverStatus(false, null, null, bundled?.ToString(), 1, false);
    }

    var installedVersion = device.GetDriverVersion();
    var updateAvailable = bundled != null && (!Version.TryParse(installedVersion, out var installed) || bundled > installed);
    return new DriverStatus(true, device.Problem, installedVersion, bundled?.ToString(), device.GetCableCount(), updateAvailable);
}

static DriverPackage? LoadBundledPackage()
{
    var package = DriverPackage.Load(AppContext.BaseDirectory);
    if (package == null)
    {
        Log.Error($"No driver package (INF with DriverVer) in {AppContext.BaseDirectory}.");
    }

    return package;
}
