using System;
using System.Windows;
using Unity;

namespace Micser.App.Infrastructure.Widgets
{
    /// <inheritdoc cref="IWidgetFactory"/>
    public class WidgetFactory : IWidgetFactory
    {
        private readonly IUnityContainer _container;

        /// <inheritdoc />
        public WidgetFactory(IUnityContainer container)
        {
            _container = container;
        }

        /// <inheritdoc />
        public virtual WidgetViewModel CreateViewModel(Type widgetVmType)
        {
            return (WidgetViewModel)_container.Resolve(widgetVmType);
        }

        /// <inheritdoc />
        public virtual Widget CreateWidget(WidgetViewModel viewModel)
        {
            var result = _container.Resolve<Widget>(viewModel.GetType().AssemblyQualifiedName);
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