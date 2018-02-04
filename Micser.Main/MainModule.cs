using System;
using Micser.Core.Views;
using Micser.Infrastructure.Extensions;
using Micser.Infrastructure.Themes;
using Micser.Main.Extensions;
using Micser.Main.ViewModels;
using Micser.Main.ViewModels.Widgets;
using Micser.Main.Views.Widgets;
using Prism.Modularity;
using Prism.Regions;
using Unity;

namespace Micser.Main
{
    public class MainModule : IModule
    {
        private readonly IUnityContainer _container;
        private readonly IRegionManager _regionManager;
        private readonly IResourceRegistry _resourceRegistry;

        public MainModule(IUnityContainer container, IRegionManager regionManager, IResourceRegistry resourceRegistry)
        {
            _container = container;
            _regionManager = regionManager;
            _resourceRegistry = resourceRegistry;
        }

        public void Initialize()
        {
            _resourceRegistry.Add(new Uri("Micser.Main;component/Themes/Generic.xaml", UriKind.Relative));
            _resourceRegistry.Add(new Uri("Micser.Main;component/Themes/WidgetPanel.xaml", UriKind.Relative));
            _resourceRegistry.Add(new Uri("Micser.Main;component/Themes/Widget.xaml", UriKind.Relative));

            _container.RegisterSingleton<IWidgetFactory, WidgetFactory>();
            _container.RegisterWidget<DeviceInputWidget, DeviceInputViewModel>();

            _container.RegisterView<MainView, MainViewModel>("MainRegion");

            _regionManager.RequestNavigate("MainRegion", new Uri(nameof(MainView), UriKind.Relative));
        }
    }
}