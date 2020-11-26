using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace Micser.Common.Extensions
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, string connectionString)
            where TContext : DbContext
        {
            var directory = Globals.AppDataFolder;

#if DEBUG
            directory = Path.Combine(directory, "Debug");
#endif

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            connectionString = connectionString.Replace(Globals.ConnectionStringFolder, directory);

            services.AddDbContextFactory<TContext>(options => options.UseSqlite(connectionString));
            return services;
        }

        public static IServiceCollection AddModules<TModule>(this IServiceCollection services, params Type[] staticModules)
            where TModule : IModule
        {
            foreach (var module in staticModules)
            {
                services.AddSingleton(typeof(IModule), module);
            }

            var executingFile = new FileInfo(Assembly.GetExecutingAssembly().Location);

            foreach (var moduleFile in executingFile.Directory.GetFiles(Globals.PluginSearchPattern))
            {
                try
                {
                    var assembly = Assembly.LoadFile(moduleFile.FullName);
                    var moduleTypes = assembly.GetExportedTypes().Where(t => typeof(TModule).IsAssignableFrom(t));

                    foreach (var moduleType in moduleTypes)
                    {
                        services.AddSingleton(typeof(IModule), moduleType);
                    }
                }
                catch (Exception ex)
                {
                    // todo
                    //Logger.Debug(ex);
                }
            }

            return services;
        }

        public static IServiceCollection AddNlog(this IServiceCollection services, string fileName)
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
                FileName = Path.Combine(Globals.AppDataFolder, fileName),
                FileNameKind = FilePathKind.Absolute
            });
            config.AddTarget(new DebuggerTarget("DebuggerTarget"));

            config.AddRuleForAllLevels("ConsoleTarget");
            config.AddRuleForAllLevels("FileTarget");
            config.AddRuleForAllLevels("DebuggerTarget");

            services.AddLogging(options => options.AddNLog(config));

            return services;
        }

        public static IApplicationBuilder UseModules<TModule>(this IApplicationBuilder app)
            where TModule : IModule
        {
            var plugins = app.ApplicationServices.GetRequiredService<IEnumerable<TModule>>();

            foreach (var plugin in plugins)
            {
                plugin.Initialize(app);
            }

            return app;
        }
    }
}