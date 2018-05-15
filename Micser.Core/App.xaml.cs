using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using CommonServiceLocator;
using Micser.Infrastructure;
using Micser.Infrastructure.Themes;
using NLog;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Unity;
using Unity.Injection;

namespace Micser.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);

            moduleCatalog.AddModule<CoreModule>();
            moduleCatalog.AddModule<InfrastructureModule>();

            LoadDynamicModules(moduleCatalog);
        }

        protected override Window CreateShell()
        {
            return ServiceLocator.Current.GetInstance<MainShell>();
        }

        protected override void InitializeModules()
        {
            var configurationService = ServiceLocator.Current.GetInstance<IConfigurationService>();
            configurationService.Load();

            base.InitializeModules();

            var resourceRegistry = ServiceLocator.Current.GetInstance<IResourceRegistry>();
            foreach (var dictionary in resourceRegistry.Items)
            {
                Current.Resources.MergedDictionaries.Add(dictionary);
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IConfigurationService, ConfigurationService>();

            var container = containerRegistry.GetContainer();
            container.RegisterType<ILogger>(new InjectionFactory((c, t, n) => LogManager.GetCurrentClassLogger()));
        }

        private static void LoadDynamicModules(IModuleCatalog moduleCatalog)
        {
            var executingFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var moduleFiles = executingFile.Directory.GetFiles("Micser.*.dll");
            foreach (var moduleFile in moduleFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFile(moduleFile.FullName);
                    var moduleTypes = assembly.GetExportedTypes().Where(t => typeof(IModule).IsAssignableFrom(t));
                    foreach (var moduleType in moduleTypes)
                    {
                        if (!moduleCatalog.Modules.Any(m => m.ModuleType == moduleType.AssemblyQualifiedName))
                        {
                            moduleCatalog.AddModule(moduleType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = ServiceLocator.Current.GetInstance<ILogger>();
                    logger.Debug(ex);
                    Debug.WriteLine(ex);
                }
            }
        }
    }
}