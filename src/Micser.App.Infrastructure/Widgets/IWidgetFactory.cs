using System;

namespace Micser.App.Infrastructure.Widgets
{
    public interface IWidgetFactory
    {
        WidgetViewModel CreateViewModel(Type widgetVmType);

        Widget CreateWidget(WidgetViewModel viewModel);

        Widget CreateWidget(WidgetDescription description);
    }
}