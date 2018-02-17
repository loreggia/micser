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

        public virtual Widget CreateWidget(object dataContext)
        {
            Widget result = null;
            WidgetViewModel vm = null;

            if (dataContext is WidgetDescription d)
            {
                result = _container.Resolve<Widget>(d.ViewModelType.FullName);
                vm = (WidgetViewModel)_container.Resolve(d.ViewModelType);
            }
            else if (dataContext is WidgetViewModel)
            {
                vm = (WidgetViewModel)dataContext;
                result = _container.Resolve<Widget>(vm.GetType().FullName);
            }

            if (result != null)
            {
                result.DataContext = vm;
                result.Loaded += (s, e) => { vm?.Initialize(); };
            }

            return result;
        }
    }
}