using Micser.App.Infrastructure.Widgets;
using Prism.Ioc;
using Prism.Unity;
using System.Windows;
using Unity;
using Unity.Injection;

namespace Micser.App.Infrastructure.Extensions
{
    public static class PrismContainerExtensions
    {
        public static void RegisterView<TView, TViewModel>(this IContainerRegistry containerRegistry)
            where TView : FrameworkElement
            where TViewModel : IViewModel
        {
            var container = containerRegistry.GetContainer();
            container.RegisterType<TView>();
            container.RegisterType<object>(typeof(TView).Name, new InjectionFactory(c =>
            {
                var view = c.Resolve<TView>();
                var viewModel = c.Resolve<TViewModel>();

                view.DataContext = viewModel;

                return view;
            }));

            //container.RegisterTypeForNavigation<TView>();
        }

        public static void RegisterWidget<TWidget, TViewModel>(this IContainerRegistry containerRegistry, string defaultName, string description)
            where TWidget : Widget
            where TViewModel : WidgetViewModel
        {
            var container = containerRegistry.GetContainer();
            container.RegisterType<Widget>(typeof(TViewModel).AssemblyQualifiedName, new InjectionFactory(c =>
            {
                var widget = c.Resolve<TWidget>();
                return widget;
            }));
            container.RegisterInstance(typeof(TViewModel).AssemblyQualifiedName, new WidgetDescription
            {
                Name = defaultName,
                Description = description,
                ViewType = typeof(TWidget),
                ViewModelType = typeof(TViewModel)
            });
        }
    }
}