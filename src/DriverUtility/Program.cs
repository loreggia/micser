using System.Security.Principal;
using Micser.DriverUtility;
using Serilog;

// Usage: Micser.DriverUtility.exe /c <device count> [/s]
//   /c  number of virtual audio cables (1..8)
//   /s  silent: no console log and no elevation check (the installer runs it elevated)

var silent = args.Any(a => a.Equals("/s", StringComparison.OrdinalIgnoreCase));
var countIndex = Array.FindIndex(args, a => a.Equals("/c", StringComparison.OrdinalIgnoreCase));
var countArgument = countIndex >= 0 && countIndex < args.Length - 1 ? args[countIndex + 1] : null;

var logConfiguration = new LoggerConfiguration()
    .WriteTo.File(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Micser", "logs", "driver-utility-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7);
if (!silent)
{
    logConfiguration.WriteTo.Console();
}

Log.Logger = logConfiguration.CreateLogger();

try
{
    if (silent)
    {
        Console.WriteLine("Configuring virtual audio cables...");
    }
    else if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
    {
        Log.Error("Managing the driver requires elevated privileges.");
        return ReturnCodes.RequiresAdminAccess;
    }

    Log.Information("Arguments: {Arguments}", args);

    if (!int.TryParse(countArgument, out var deviceCount))
    {
        Log.Error("Invalid or missing device count (/c): {Value}", countArgument);
        return ReturnCodes.InvalidParameter;
    }

    var result = DriverController.SetDeviceCountAndReload(deviceCount);
    if (result != ReturnCodes.Success)
    {
        return result;
    }

    if (!await DeviceRenamer.RenameDevicesAsync(Math.Clamp(deviceCount, 1, DriverGlobals.MaxDeviceCount)))
    {
        Log.Error("Renaming the devices failed.");
    }

    Log.Information("Done.");
    return ReturnCodes.Success;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Configuring the driver failed.");
    return ReturnCodes.UnknownError;
}
finally
{
    await Log.CloseAndFlushAsync();
}
