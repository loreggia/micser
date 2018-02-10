using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using CommonServiceLocator;
using Micser.Infrastructure;
using Micser.Infrastructure.Themes;
using Prism.Ioc;
using Prism.Modularity;

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
            base.InitializeModules();

            var resourceRegistry = ServiceLocator.Current.GetInstance<IResourceRegistry>();
            foreach (var dictionary in resourceRegistry.Items)
            {
                Current.Resources.MergedDictionaries.Add(dictionary);
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
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
                    // todo logger
                    Debug.WriteLine(ex);
                }
            }
        }
    }
}