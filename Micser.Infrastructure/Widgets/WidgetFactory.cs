using System;
using System.Windows;
using Unity;

namespace Micser.Infrastructure.Widgets
{
    public class WidgetFactory : IWidgetFactory
    {
        private readonly IUnityContainer _container;

        public WidgetFactory(IUnityContainer container)
        {
            _container = container;
        }

        public virtual WidgetViewModel CreateViewModel(Type widgetVmType)
        {
            return (WidgetViewModel)_container.Resolve(widgetVmType);
        }

        public virtual Widget CreateWidget(WidgetViewModel viewModel)
        {
            var result = _container.Resolve<Widget>(viewModel.GetType().FullName);
            result.DataContext = viewModel;
            result.Loaded += OnWidgetLoaded;
            return result;
        }

        public virtual Widget CreateWidget(WidgetDescription d)
        {
            var result = _container.Resolve<Widget>(d.ViewModelType.FullName);
            result.DataContext = (WidgetViewModel)_container.Resolve(d.ViewModelType);
            result.Loaded += OnWidgetLoaded;
            return result;
        }

        protected virtual void OnWidgetLoaded(object sender, RoutedEventArgs e)
        {
            var widget = (Widget)sender;
            var viewModel = widget.DataContext as WidgetViewModel;
            viewModel?.Initialize();
        }
    }
}