using Micser.Common;
using Micser.Plugins.Main.Driver;
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
                Layout = @"${date:format=HH\:mm\:ss} ${level} ${message} ${exception}",
                DetectConsoleAvailable = true
            });
            config.AddTarget(new FileTarget("FileTarget")
            {
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                ArchiveOldFileOnStartup = true,
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
                return -1;
            }

            logger.Info("Starting...");

            try
            {
                var arguments = new ArgumentDictionary(Globals.DriverUtility.ParamNameChars, args);
                logger.Info("Arguments: " + arguments);

                var deviceId = arguments[Globals.DriverUtility.DeviceId];

                if (string.IsNullOrEmpty(deviceId))
                {
                    logger.Error("No device name provided.");
                    return -1;
                }

                var installer = new DriverInstaller(deviceId);

                if (arguments.HasFlag(Globals.DriverUtility.InstallFlag))
                {
                    installer.Install();
                }
                else if (arguments.HasFlag(Globals.DriverUtility.UninstallFlag))
                {
                    installer.Uninstall();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return -1;
            }

            logger.Info("Finished");
            return 0;
        }
    }
}