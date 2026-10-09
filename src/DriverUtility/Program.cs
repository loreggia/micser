using System.Security.Principal;
using System.Text.Json;
using Micser.DriverUtility;

// Manages the virtual audio cable driver. The commands except "status" need administrator rights.
//
//   Micser.DriverUtility status                  prints the driver status as JSON (DriverStatus)
//   Micser.DriverUtility install [--count <n>]   installs the driver package next to the utility, or updates an existing device
//   Micser.DriverUtility update                  installs the driver package next to the utility on the existing device
//   Micser.DriverUtility set-count <n>           sets the number of cables (1-16) and restarts the device
//   Micser.DriverUtility set-layout <cable> <layout>
//                                                sets a cable's layout (stereo, 5.1 or 7.1) and restarts the device
//   Micser.DriverUtility sync-formats            sets the cables' endpoints to their layout's format (no administrator rights needed)
//   Micser.DriverUtility uninstall               removes the device, the driver and the installation in Program Files
//
// Windows keeps an endpoint's device format across device restarts, so after a layout change the endpoints have to be set to the new
// format; the commands that restart the device do that, unless a reboot is needed first ("sync-formats" afterwards).
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
    Console.Error.WriteLine(
        "Usage: Micser.DriverUtility status | install [--count <n>] | update | set-count <n> | set-layout <cable> stereo|5.1|7.1 | sync-formats | uninstall"
    );
    return InvalidArguments;
}

if (command == "status")
{
    Console.WriteLine(JsonSerializer.Serialize(GetStatus(), DriverStatusJsonContext.Default.DriverStatus));
    return Success;
}

if (command == "sync-formats")
{
    try
    {
        using var device = VacDevice.Find();
        if (device is not { IsPresent: true })
        {
            Log.Error("The driver isn't installed.");
            return NotFound;
        }

        return SyncFormats(device) ? Success : Failure;
    }
    catch (Exception ex)
    {
        Log.Error(ex.ToString());
        return Failure;
    }
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
            if (!needReboot)
            {
                using var device = VacDevice.Find();
                if (device is { IsPresent: true })
                {
                    SyncFormats(device);
                }
            }

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
            if (!needReboot)
            {
                using var device = VacDevice.Find();
                if (device is { IsPresent: true })
                {
                    SyncFormats(device);
                }
            }

            break;
        }

        case "set-count":
        {
            if (
                args.Length < 2
                || !int.TryParse(args[1], out var count)
                || count < 1
                || count > VacDevice.MaxCableCount
            )
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
            if (!needReboot)
            {
                SyncFormats(device);
            }

            break;
        }

        case "set-layout":
        {
            using var device = VacDevice.Find();
            if (device is not { IsPresent: true })
            {
                Log.Error("The driver isn't installed.");
                return NotFound;
            }

            var cableCount = device.GetCableCount();
            if (
                args.Length < 3
                || !int.TryParse(args[1], out var cable)
                || cable < 1
                || cable > cableCount
                || !CableLayouts.TryParse(args[2], out var layout)
            )
            {
                Log.Error($"set-layout needs a cable from 1 to {cableCount} and a layout: stereo, 5.1 or 7.1.");
                return InvalidArguments;
            }

            Log.Info($"Setting cable {cable} to {layout.GetName()}");
            device.SetCableLayout(cable, layout);
            needReboot = device.Restart();
            if (!needReboot && !SyncFormats(device))
            {
                return Failure;
            }

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
        return new DriverStatus(false, null, null, bundled?.ToString(), 1, false, []);
    }

    var installedVersion = device.GetDriverVersion();
    var updateAvailable =
        bundled != null && (!Version.TryParse(installedVersion, out var installed) || bundled > installed);
    var layouts = device.GetCableLayouts();
    var endpoints = CableEndpoints.Find(device.GetInstanceId());
    var cables = layouts
        .Select(
            (layout, index) =>
                new CableStatus(layout.GetName(), CableEndpoints.CableMatches(endpoints, index + 1, layout))
        )
        .ToList();
    return new DriverStatus(
        true,
        device.Problem,
        installedVersion,
        bundled?.ToString(),
        layouts.Count,
        updateAvailable,
        cables
    );
}

// Sets the cables' endpoints to the format of their layout; they appear a moment after the device restarted.
static bool SyncFormats(VacDevice device) =>
    CableEndpoints.Sync(device.GetInstanceId(), device.GetCableLayouts(), TimeSpan.FromSeconds(15));

static DriverPackage? LoadBundledPackage()
{
    var package = DriverPackage.Load(AppContext.BaseDirectory);
    if (package == null)
    {
        Log.Error($"No driver package (INF with DriverVer) in {AppContext.BaseDirectory}.");
    }

    return package;
}
