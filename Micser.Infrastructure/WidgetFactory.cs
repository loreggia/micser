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

        public virtual Widget CreateWidget(object dataContext)
        {
            Widget result = null;

            if (dataContext is WidgetDescription d)
            {
                result = _container.Resolve<Widget>(d.Type.FullName);
            }
            else if (dataContext is WidgetViewModel vm)
            {
                result = _container.Resolve<Widget>(vm.GetType().FullName);
            }

            return result;
        }
    }
}