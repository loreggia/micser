using System;
using Micser.Core.Views;
using Micser.Infrastructure.Extensions;
using Micser.Main.ViewModels;
using Prism.Modularity;
using Prism.Regions;
using Unity;

namespace Micser.Main
{
    public class MainModule : IModule
    {
        private readonly IUnityContainer _container;
        private readonly IRegionManager _regionManager;

        public MainModule(IUnityContainer container, IRegionManager regionManager)
        {
            _container = container;
            _regionManager = regionManager;
        }

        public void Initialize()
        {
            _container.RegisterView<MainView, MainViewModel>("MainRegion");

            _regionManager.RequestNavigate("MainRegion", new Uri(nameof(MainView), UriKind.Relative));
        }
    }
}