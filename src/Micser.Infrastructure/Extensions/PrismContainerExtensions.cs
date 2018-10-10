using Micser.Infrastructure.Widgets;
using Prism.Ioc;
using Prism.Regions;
using Prism.Unity;
using System.Windows;
using Unity;
using Unity.Injection;

namespace Micser.Infrastructure.Extensions
{
    public static class PrismContainerExtensions
    {
        public static void RegisterView<TView, TViewModel>(this IContainerRegistry containerRegistry, string regionName)
                    where TView : FrameworkElement
            where TViewModel : IViewModel
        {
            var container = containerRegistry.GetContainer();
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

        public static void RegisterWidget<TWidget, TViewModel>(this IContainerRegistry containerRegistry, string defaultName)
            where TWidget : Widget
            where TViewModel : WidgetViewModel
        {
            var container = containerRegistry.GetContainer();
            container.RegisterType<Widget>(typeof(TViewModel).FullName, new InjectionFactory(c =>
            {
                var widget = c.Resolve<TWidget>();
                return widget;
            }));
            container.RegisterInstance(typeof(TWidget).FullName, new WidgetDescription
            {
                Name = defaultName,
                ViewType = typeof(TWidget),
                ViewModelType = typeof(TViewModel)
            });
        }
    }
}