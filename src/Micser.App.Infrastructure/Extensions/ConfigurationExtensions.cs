using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Micser.App.Infrastructure.Widgets;

namespace Micser.App.Infrastructure.Extensions
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Registers a view and it's corresponding view model.
        /// </summary>
        /// <typeparam name="TView">The view type.</typeparam>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        public static void RegisterView<TView, TViewModel>(this IServiceCollection services)
            where TView : FrameworkElement
            where TViewModel : IViewModel
        {
            services.AddTransient<TView>();
            // TODO
            //services.AddTransient<object>(sp =>
            //{
            //    var view = sp.GetRequiredService<TView>();
            //    var viewModel = sp.GetRequiredService<TViewModel>();

            //    view.DataContext = viewModel;

            //    return view;
            //}, typeof(TView).Name);

            //container.RegisterTypeForNavigation<TView>();
        }

        /// <summary>
        /// Creates a widget registration so the widget is shown in the widget tool box and can be loaded in the widget panel.
        /// </summary>
        /// <typeparam name="TWidget">The widget view type.</typeparam>
        /// <typeparam name="TViewModel">The widget view model type.</typeparam>
        /// <param name="services"></param>
        /// <param name="defaultName">The default name that is shown in the widget tool box and used when creating a widget.</param>
        /// <param name="description">A description that is shown in the widget tool box.</param>
        public static void RegisterWidget<TWidget, TViewModel>(this IServiceCollection services, object defaultName, object description)
            where TWidget : Widget
            where TViewModel : WidgetViewModel
        {
            // TODO
            //container.RegisterFactory<Widget>(c =>
            //{
            //    var widget = c.Resolve<TWidget>();
            //    return widget;
            //}, typeof(TViewModel).AssemblyQualifiedName);
            //container.RegisterInstance(new WidgetDescription
            //{
            //    Name = defaultName,
            //    Description = description,
            //    ViewType = typeof(TWidget),
            //    ViewModelType = typeof(TViewModel)
            //}, typeof(TViewModel).AssemblyQualifiedName);
        }
    }
}