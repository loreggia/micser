﻿using CommonServiceLocator;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Themes;
using Micser.Infrastructure;
using NLog;
using NLog.Config;
using NLog.Targets;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Micser.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class Application
    {
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);

            moduleCatalog.AddModule<AppModule>();
            moduleCatalog.AddModule<InfrastructureModule>();

            LoadPlugins(moduleCatalog);
        }

        protected override Window CreateShell()
        {
            return GetService<MainShell>();
        }

        protected override void InitializeModules()
        {
            SetStatus("Initializing...");

            base.InitializeModules();

            var resourceRegistry = GetService<IResourceRegistry>();
            foreach (var dictionary in resourceRegistry.Items)
            {
                Current.Resources.MergedDictionaries.Add(dictionary);
            }

            var eventAggregator = GetService<IEventAggregator>();
            var modulesLoadedEvent = eventAggregator.GetEvent<ApplicationEvents.ModulesLoaded>();
            modulesLoadedEvent.Publish();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Exiting...");
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            SetStatus("Ready");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Directory.CreateDirectory(Globals.AppDataFolder);

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
                FileName = Path.Combine(Globals.AppDataFolder, "Micser.App.log"),
                FileNameKind = FilePathKind.Absolute
            });
            config.AddRuleForAllLevels("ConsoleTarget");
            config.AddRuleForAllLevels("FileTarget");

            LogManager.Configuration = config;
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Starting...");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        private static T GetService<T>()
        {
            return ServiceLocator.Current.GetInstance<T>();
        }

        private static void LoadPlugins(IModuleCatalog moduleCatalog)
        {
            SetStatus("Loading plugins...");

            var executingFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var moduleFiles = executingFile.Directory.GetFiles(Globals.PluginSearchPattern);
            foreach (var moduleFile in moduleFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFile(moduleFile.FullName);
                    var moduleTypes = assembly.GetExportedTypes().Where(t => typeof(IModule).IsAssignableFrom(t));
                    foreach (var moduleType in moduleTypes)
                    {
                        if (moduleCatalog.Modules.All(m => m.ModuleType != moduleType.AssemblyQualifiedName))
                        {
                            moduleCatalog.AddModule(moduleType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = GetService<ILogger>();
                    logger.Debug(ex);
                    Debug.WriteLine(ex);
                }
            }
        }

        private static void SetStatus(string text)
        {
            var eventAggregator = GetService<IEventAggregator>();
            var statusChangeEvent = eventAggregator.GetEvent<ApplicationEvents.StatusChange>();
            statusChangeEvent.Publish(text);
        }
    }
}