using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Micser.App.Infrastructure.Widgets
{
    /// <inheritdoc cref="IWidgetFactory"/>
    public class WidgetFactory : IWidgetFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <inheritdoc />
        public WidgetFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public virtual WidgetViewModel CreateViewModel(Type widgetVmType)
        {
            return (WidgetViewModel)_serviceProvider.GetRequiredService(widgetVmType);
        }

        /// <inheritdoc />
        public virtual Widget CreateWidget(WidgetViewModel viewModel)
        {
            //var result = _container.GetRequiredService<Widget>(viewModel.GetType().AssemblyQualifiedName);
            var result = _serviceProvider.GetRequiredService<Widget>();
            result.DataContext = viewModel;
            result.Loaded += OnWidgetLoaded;
            return result;
        }

        /// <summary>
        /// Handles a widget's <see cref="FrameworkElement.Loaded"/> event.
        /// </summary>
        protected virtual void OnWidgetLoaded(object sender, RoutedEventArgs e)
        {
            var widget = (Widget)sender;
            var viewModel = widget.DataContext as WidgetViewModel;
            viewModel?.Initialize();
        }
    }
}