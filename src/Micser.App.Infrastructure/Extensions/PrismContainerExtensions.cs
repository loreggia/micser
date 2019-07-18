using Micser.App.Infrastructure.Widgets;
using Prism.Ioc;
using Prism.Unity;
using System.Windows;
using Unity;
using Unity.Injection;

namespace Micser.App.Infrastructure.Extensions
{
    /// <summary>
    /// Provides helper extension methods for the <see cref="IContainerRegistry"/> class.
    /// </summary>
    public static class PrismContainerExtensions
    {
        /// <summary>
        /// Registers a view and it's corresponding view model.
        /// </summary>
        /// <typeparam name="TView">The view type.</typeparam>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        /// <param name="containerRegistry"></param>
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

        /// <summary>
        /// Creates a widget registration so the widget is shown in the widget tool box and can be loaded in the widget panel.
        /// </summary>
        /// <typeparam name="TWidget">The widget view type.</typeparam>
        /// <typeparam name="TViewModel">The widget view model type.</typeparam>
        /// <param name="containerRegistry"></param>
        /// <param name="defaultName">The default name that is shown in the widget tool box and used when creating a widget.</param>
        /// <param name="description">A description that is shown in the widget tool box.</param>
        public static void RegisterWidget<TWidget, TViewModel>(this IContainerRegistry containerRegistry, object defaultName, object description)
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