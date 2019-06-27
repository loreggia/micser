using Micser.Common;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace Micser.Engine
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        internal static void Main(params string[] arguments)
        {
            try
            {
                // try setting process priority to high
                using (var process = Process.GetCurrentProcess())
                {
                    process.PriorityClass = ProcessPriorityClass.High;
                }
            }
            catch
            {
                // ignore
            }

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
            config.AddTarget(new DebuggerTarget("DebuggerTarget"));

            config.AddRuleForAllLevels("ConsoleTarget");
            config.AddRuleForAllLevels("FileTarget");
            config.AddRuleForAllLevels("DebuggerTarget");

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