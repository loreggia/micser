using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

            void ConfigureDbContextOptions(DbContextOptionsBuilder options)
            {
                options.UseSqlite(connectionString);
            }

            //services.AddDbContext<TContext>(ConfigureDbContextOptions, ServiceLifetime.Transient);
            services.AddDbContextFactory<TContext>(ConfigureDbContextOptions);
            return services;
        }

        public static IServiceCollection AddModules<TModule>(this IServiceCollection services, IConfiguration configuration, params Type[] staticModules)
            where TModule : IModule
        {
            void AddModule(Type moduleType)
            {
                if (Activator.CreateInstance(moduleType) is IModule module)
                {
                    services.AddSingleton(module);
                    module.ConfigureServices(services, configuration);
                }
                else
                {
                    throw new InvalidOperationException($"Failed to activate module. Type: {moduleType}");
                }
            }

            foreach (var module in staticModules)
            {
                AddModule(module);
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
                        try
                        {
                            AddModule(moduleType);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Error during module activation. Assembly: {moduleFile} Module: {moduleType}", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error during module registration. Assembly: {moduleFile}.", ex);
                }
            }

            return services;
        }

        public static IServiceCollection AddNlog(this IServiceCollection services, string fileName)
        {
            var config = new LoggingConfiguration();
            config.AddTarget(new FileTarget("FileTarget")
            {
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence,
                ArchiveOldFileOnStartup = true,
                MaxArchiveFiles = 10,
                FileName = Path.Combine(Globals.AppDataFolder, fileName),
                FileNameKind = FilePathKind.Absolute
            });
            config.AddRuleForAllLevels("FileTarget");

            services.AddLogging(options =>
            {
                options.AddNLog(config);
            });

            return services;
        }
    }
}