using System;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Provides methods to instantiate widgets and widget view models.
    /// </summary>
    public interface IWidgetFactory
    {
        WidgetViewModel CreateViewModel(Type widgetVmType);

        Widget CreateWidget(WidgetViewModel viewModel);
    }
}