using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common.Audio;
using Micser.Common.Modules;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace Micser.Common.Extensions
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddAudioModule<TModule>(this IServiceCollection services, ModuleDescription description)
            where TModule : class, IAudioModule
        {
            services.AddTransient<IAudioModule, TModule>();
            services.AddSingleton(description);

            return services;
        }

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

        public static IServiceCollection AddPlugins<TPlugin>(this IServiceCollection services, IConfiguration configuration, params Type[] staticPlugins)
            where TPlugin : IPlugin
        {
            void AddPlugin(Type pluginType)
            {
                if (Activator.CreateInstance(pluginType) is IPlugin plugin)
                {
                    services.AddSingleton(plugin);
                    plugin.ConfigureServices(services, configuration);
                }
                else
                {
                    throw new InvalidOperationException($"Failed to activate plugin. Type: {pluginType}");
                }
            }

            foreach (var plugin in staticPlugins)
            {
                AddPlugin(plugin);
            }

            var executingFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var directory = executingFile.Directory;

            if (directory == null)
            {
                throw new InvalidOperationException("Could not access plugins directory.");
            }

            foreach (var pluginFile in directory.GetFiles(Globals.PluginSearchPattern))
            {
                try
                {
                    var assembly = Assembly.LoadFile(pluginFile.FullName);
                    var pluginTypes = assembly.GetExportedTypes().Where(t => typeof(TPlugin).IsAssignableFrom(t));

                    foreach (var pluginType in pluginTypes)
                    {
                        try
                        {
                            AddPlugin(pluginType);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Error during plugin activation. Assembly: {pluginFile} Plugin: {pluginType}", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error during plugin registration. Assembly: {pluginFile}.", ex);
                }
            }

            return services;
        }

        public static void UsePlugins(this IServiceProvider services)
        {
            var plugins = services.GetRequiredService<IEnumerable<IPlugin>>();

            foreach (var plugin in plugins)
            {
                plugin.Initialize(services);
            }
        }
    }
}