using Micser.Main.Controls;
using Micser.Main.ViewModels.Widgets;
using Unity;
using Unity.Injection;

namespace Micser.Main.Extensions
{
    public static class ContainerExtensions
    {
        public static void RegisterWidget<TWidget, TViewModel>(this IUnityContainer container)
            where TWidget : Widget
            where TViewModel : WidgetViewModel
        {
            container.RegisterType<Widget>(typeof(TViewModel).Name, new InjectionFactory(c =>
             {
                 var widget = c.Resolve<TWidget>();
                 //widget.DataContext = c.Resolve<TViewModel>();
                 return widget;
             }));
        }
    }
}