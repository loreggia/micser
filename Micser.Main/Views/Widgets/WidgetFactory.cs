using Micser.Main.Controls;
using Micser.Main.ViewModels.Widgets;
using Unity;

namespace Micser.Main.Views.Widgets
{
    public class WidgetFactory : IWidgetFactory
    {
        private readonly IUnityContainer _container;

        public WidgetFactory(IUnityContainer container)
        {
            _container = container;
        }

        public virtual Widget CreateWidget(WidgetViewModel viewModel)
        {
            var widget = _container.Resolve<Widget>(viewModel.GetType().Name);
            widget.DataContext = viewModel;
            return widget;
        }
    }
}