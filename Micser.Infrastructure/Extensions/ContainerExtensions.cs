using System.Windows;
using Micser.Infrastructure.Controls;
using Micser.Infrastructure.ViewModels;
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

        public static void RegisterWidget<TWidget, TViewModel>(this IUnityContainer container, string defaultName = null)
            where TWidget : Widget
            where TViewModel : WidgetViewModel
        {
            container.RegisterType<Widget>(typeof(TWidget).FullName, new InjectionFactory(c =>
            {
                var widget = c.Resolve<TWidget>();
                widget.DataContext = c.Resolve<TViewModel>();
                return widget;
            }));
            container.RegisterInstance(typeof(TWidget).FullName, new WidgetDescription
            {
                Name = defaultName,
                Type = typeof(TWidget)
            });
        }
    }
}