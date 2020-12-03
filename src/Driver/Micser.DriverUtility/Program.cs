using System;
using System.Diagnostics;
using System.IO;
using Micser.Common;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Micser.DriverUtility
{
    internal class Program
    {
        private static void InitLogging(bool isInstallation)
        {
            var config = new LoggingConfiguration();

            if (!isInstallation)
            {
                config.AddTarget(new ColoredConsoleTarget("ConsoleTarget")
                {
                    Layout = @"${date:format=HH\:mm\:ss} ${level} ${message} ${exception:format=tostring}",
                    DetectConsoleAvailable = true
                });
                config.AddRuleForAllLevels("ConsoleTarget");
            }

            config.AddTarget(new FileTarget("FileTarget")
            {
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                ArchiveOldFileOnStartup = true,
                Layout = @"${date:format=HH\:mm\:ss} ${level} ${message} ${exception:format=tostring}",
                MaxArchiveFiles = 10,
                FileName = Path.Combine(Globals.AppDataFolder, "Micser.DriverUtility.log"),
                FileNameKind = FilePathKind.Absolute
            });
            config.AddRuleForAllLevels("FileTarget");

            LogManager.Configuration = config;
        }

        private static int Main(string[] args)
        {
            var result = MainInternal(args);

            if (Debugger.IsAttached)
            {
                Console.ReadLine();
            }

            return result;
        }

        private static int MainInternal(string[] args)
        {
            var arguments = new ArgumentDictionary(Globals.DriverUtility.ArgumentNameChars, args);
            var silent = arguments.HasFlag(Globals.DriverUtility.Arguments.Silent);
            InitLogging(silent);

            if (silent)
            {
                // In silent mode the console log is deactivated; only show the following:
                Console.WriteLine("Configuring virtual audio cable...");
            }

            var logger = LogManager.GetCurrentClassLogger();

            // TODO the check doesn't work during msi installation..
            if (!silent && !UacHelper.IsProcessElevated)
            {
                logger.Error("The process must have elevated privileges to manage driver installation.");
                return Globals.DriverUtility.ReturnCodes.RequiresAdminAccess;
            }

            logger.Info("Starting...");

            try
            {
                logger.Info("Arguments: " + arguments);

                var sDeviceCount = arguments[Globals.DriverUtility.Arguments.DeviceCount];

                if (string.IsNullOrEmpty(sDeviceCount) || !int.TryParse(sDeviceCount, out var deviceCount))
                {
                    logger.Error($"Invalid or missing device count argument '{Globals.DriverUtility.ArgumentNameChars[0]}{Globals.DriverUtility.Arguments.DeviceCount}' provided: '{sDeviceCount}'.");
                    return Globals.DriverUtility.ReturnCodes.InvalidParameter;
                }

                var controller = new DriverController();
                var result = controller.SetDeviceSettingsAndReload(deviceCount);

                if (result != Globals.DriverUtility.ReturnCodes.Success)
                {
                    logger.Error($"{nameof(DriverController.SetDeviceSettingsAndReload)} returned {result}");
                    return result;
                }

                using (var deviceService = new DeviceService())
                {
                    var renameResult = deviceService.RenameDevices(deviceCount).GetAwaiter().GetResult();
                    if (!renameResult)
                    {
                        logger.Error("Renaming failed.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return Globals.DriverUtility.ReturnCodes.UnknownError;
            }

            logger.Info("Success");
            return Globals.DriverUtility.ReturnCodes.Success;
        }
    }
}