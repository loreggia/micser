using Micser.App.Infrastructure.Widgets;
using Micser.Common;
using Micser.Common.Extensions;
using System.Windows;

namespace Micser.App.Infrastructure.Extensions
{
    /// <summary>
    /// Provides helper extension methods for the <see cref="IContainerProvider"/> class.
    /// </summary>
    public static class ContainerProviderExtensions
    {
        /// <summary>
        /// Registers a view and it's corresponding view model.
        /// </summary>
        /// <typeparam name="TView">The view type.</typeparam>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        /// <param name="container"></param>
        public static void RegisterView<TView, TViewModel>(this IContainerProvider container)
            where TView : FrameworkElement
            where TViewModel : IViewModel
        {
            container.RegisterType<TView>();
            container.RegisterFactory<object>(c =>
            {
                var view = c.Resolve<TView>();
                var viewModel = c.Resolve<TViewModel>();

                view.DataContext = viewModel;

                return view;
            }, typeof(TView).Name);

            //container.RegisterTypeForNavigation<TView>();
        }

        /// <summary>
        /// Creates a widget registration so the widget is shown in the widget tool box and can be loaded in the widget panel.
        /// </summary>
        /// <typeparam name="TWidget">The widget view type.</typeparam>
        /// <typeparam name="TViewModel">The widget view model type.</typeparam>
        /// <param name="container"></param>
        /// <param name="defaultName">The default name that is shown in the widget tool box and used when creating a widget.</param>
        /// <param name="description">A description that is shown in the widget tool box.</param>
        public static void RegisterWidget<TWidget, TViewModel>(this IContainerProvider container, object defaultName, object description)
            where TWidget : Widget
            where TViewModel : WidgetViewModel
        {
            container.RegisterFactory<Widget>(c =>
            {
                var widget = c.Resolve<TWidget>();
                return widget;
            }, typeof(TViewModel).AssemblyQualifiedName);
            container.RegisterInstance(new WidgetDescription
            {
                Name = defaultName,
                Description = description,
                ViewType = typeof(TWidget),
                ViewModelType = typeof(TViewModel)
            }, typeof(TViewModel).AssemblyQualifiedName);
        }
    }
}