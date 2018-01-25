using System.Windows;
using Prism.Regions;
using Unity;
using Unity.Injection;

namespace Micser.Infrastructure.Extensions
{
    public static class ContainerExtensions
    {
        public static void RegisterView<TView, TViewModel>(this IUnityContainer container, string regionName = null)
            where TView : FrameworkElement
            where TViewModel : IViewModel
        {
            container.RegisterType<TView>("default");
            container.RegisterType<TView>(new InjectionFactory(c =>
            {
                var view = c.Resolve<TView>("default");
                var viewModel = c.Resolve<TViewModel>();

                view.DataContext = viewModel;

                return view;
            }));

            if (!string.IsNullOrEmpty(regionName))
            {
                var regionManager = container.Resolve<IRegionManager>();
                regionManager.RegisterViewWithRegion(regionName, typeof(TView));
            }
        }
    }
}