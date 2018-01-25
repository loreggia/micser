using System.Windows;
using CommonServiceLocator;
using Micser.Infrastructure;
using Micser.Infrastructure.Themes;
using Micser.Main;
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
            moduleCatalog.AddModule<MainModule>();
        }

        protected override Window CreateShell()
        {
            return ServiceLocator.Current.GetInstance<MainShell>();
        }

        protected override void InitializeModules()
        {
            base.InitializeModules();

            var resourceRegistry = ServiceLocator.Current.GetInstance<IResourceRegistry>();
            foreach (var uri in resourceRegistry.Items)
            {
                Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = uri
                });
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}