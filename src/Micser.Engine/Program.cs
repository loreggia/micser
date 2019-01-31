using Micser.Common;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace Micser.Engine
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        internal static void Main(params string[] arguments)
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
                FileName = Path.Combine(Globals.AppDataFolder, "Micser.Engine.log"),
                FileNameKind = FilePathKind.Absolute
            });
            config.AddRuleForAllLevels("ConsoleTarget");
            config.AddRuleForAllLevels("FileTarget");

            LogManager.Configuration = config;

            var service = new MicserService();

            if (arguments.Any(a => string.Equals("manual", a, StringComparison.InvariantCultureIgnoreCase)))
            {
                service.ManualStart();

                while (true)
                {
                    Thread.Sleep(100);
                }
            }

            ServiceBase.Run(new ServiceBase[] { service });
        }
    }
}