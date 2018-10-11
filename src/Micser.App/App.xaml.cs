using CommonServiceLocator;
using Micser.Infrastructure;
using Micser.Infrastructure.Themes;
using NLog;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Unity;
using Unity.Injection;

namespace Micser.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
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

            var configurationService = GetService<IConfigurationService>();
            configurationService.Load();

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

        protected override void OnInitialized()
        {
            base.OnInitialized();
            SetStatus("Ready");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IConfigurationService, ConfigurationService>();

            var container = containerRegistry.GetContainer();
            container.RegisterType<ILogger>(new InjectionFactory((c, t, n) => LogManager.GetCurrentClassLogger()));
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