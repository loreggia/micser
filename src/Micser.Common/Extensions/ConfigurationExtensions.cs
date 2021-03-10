using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common.Audio;
using Micser.Common.Modules;
using Micser.Common.Settings;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace Micser.Common.Extensions
{
    /// <summary>
    /// Provides extension methods for service configurations.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Registers an audio module.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name/type of the module. Corresponds to the name defined for this module's widget.</param>
        /// <typeparam name="TModule">The module's type.</typeparam>
        public static IServiceCollection AddAudioModule<TModule>(this IServiceCollection services, string name)
            where TModule : class, IAudioModule
        {
            services.AddTransient<TModule>();
            services.AddSingleton(new ModuleDefinition(name, typeof(TModule)));

            return services;
        }

        /// <summary>
        /// Adds default logging via NLog.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="fileName">The file name of the log file to write to.</param>
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

            services.AddLogging(options => options.AddNLog(config));

            return services;
        }

        /// <summary>
        /// Registers all available plugins. Searches for plugin files using the <see cref="Globals.PluginSearchPattern"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The app configuration.</param>
        /// <param name="staticPlugins">An array of known plugin types to load.</param>
        /// <typeparam name="TPlugin">The type of plugins to add.</typeparam>
        /// <exception cref="InvalidOperationException">Plugin registration failed.</exception>
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

        /// <summary>
        /// Registers a setting definition.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="setting">The setting definition.</param>
        public static IServiceCollection AddSetting(this IServiceCollection services, SettingDefinition setting)
        {
            services.AddSingleton(setting);
            return services;
        }

        /// <summary>
        /// Loads the previously registered plugins. Requires a call to <see cref="AddPlugins{TPlugin}"/> during service configuration.
        /// </summary>
        /// <param name="services">The service provider.</param>
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