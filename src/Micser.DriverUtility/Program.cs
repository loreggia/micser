using Micser.Common;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.IO;

namespace Micser.DriverUtility
{
    internal class Program
    {
        private static void InitLogging()
        {
            var config = new LoggingConfiguration();
            config.AddTarget(new ColoredConsoleTarget("ConsoleTarget")
            {
                Layout = @"${date:format=HH\:mm\:ss} ${level} ${message} ${exception:format=tostring}",
                DetectConsoleAvailable = true
            });
            config.AddTarget(new FileTarget("FileTarget")
            {
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                ArchiveOldFileOnStartup = true,
                Layout = @"${date:format=HH\:mm\:ss} ${level} ${message} ${exception:format=tostring}",
                MaxArchiveFiles = 10,
                FileName = Path.Combine(Globals.AppDataFolder, "Micser.DriverUtility.log"),
                FileNameKind = FilePathKind.Absolute
            });
            config.AddRuleForAllLevels("ConsoleTarget");
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
            InitLogging();

            var logger = LogManager.GetCurrentClassLogger();

            if (!UacHelper.IsProcessElevated)
            {
                logger.Error("The process must have elevated privileges to manage driver installation.");
                return Globals.DriverUtility.ReturnCodes.RequiresAdminAccess;
            }

            logger.Info("Starting...");

            try
            {
                var arguments = new ArgumentDictionary(Globals.DriverUtility.ParamNameChars, args);
                logger.Info("Arguments: " + arguments);

                var sDeviceCount = arguments[Globals.DriverUtility.DeviceCount];

                if (string.IsNullOrEmpty(sDeviceCount) || !uint.TryParse(sDeviceCount, out var deviceCount))
                {
                    logger.Error($"Invalid or missing device count argument '{Globals.DriverUtility.ParamNameChars[0]}{Globals.DriverUtility.DeviceCount}' provided: '{sDeviceCount}'.");
                    return Globals.DriverUtility.ReturnCodes.InvalidParameter;
                }

                var controller = new DriverController();
                var result = controller.SetDeviceSettingsAndReload(deviceCount);

                if (result != Globals.DriverUtility.ReturnCodes.Success)
                {
                    logger.Error($"{nameof(DriverController.SetDeviceSettingsAndReload)} returned {result}");
                    return result;
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