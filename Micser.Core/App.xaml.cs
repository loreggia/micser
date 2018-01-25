using System;
using System.Linq;
using System.Windows;
using CommonServiceLocator;
using Micser.Infrastructure;
using Micser.Main;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Unity;

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
            moduleCatalog.AddModule<MainModule>();
        }

        protected override Window CreateShell()
        {
            return ServiceLocator.Current.GetInstance<MainShell>();
        }

        protected override void InitializeModules()
        {
            base.InitializeModules();

            var resourceModules = Container.GetContainer().ResolveAll(typeof(IModuleWithResources))?.Cast<IModuleWithResources>().ToArray();
            if (resourceModules != null)
            {
                foreach (var module in resourceModules)
                {
                    var resources = module.ResourcePaths;
                    foreach (var resource in resources)
                    {
                        Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                        {
                            Source = new Uri(resource, UriKind.Relative)
                        });
                    }
                }
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}