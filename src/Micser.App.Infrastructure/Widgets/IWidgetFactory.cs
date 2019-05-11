using System;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Provides methods to instantiate widgets and widget view models.
    /// </summary>
    public interface IWidgetFactory
    {
        /// <summary>
        /// Creates a <see cref="WidgetViewModel"/> instance of the specified type.
        /// </summary>
        WidgetViewModel CreateViewModel(Type widgetVmType);

        /// <summary>
        /// Creates a corresponding widget view for the specified view model.
        /// </summary>
        Widget CreateWidget(WidgetViewModel viewModel);
    }
}