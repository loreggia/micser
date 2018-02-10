using Micser.Infrastructure.Controls;
using Unity;

namespace Micser.Infrastructure
{
    public class WidgetFactory : IWidgetFactory
    {
        private readonly IUnityContainer _container;

        public WidgetFactory(IUnityContainer container)
        {
            _container = container;
        }

        public virtual Widget CreateWidget(WidgetDescription description)
        {
            var widget = _container.Resolve<Widget>(description.Type.FullName);
            widget.Header = description.Name;
            return widget;
        }
    }
}