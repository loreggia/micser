using System;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Describes a widget to be added to the widget registry.
    /// </summary>
    public class WidgetDescription
    {
        /// <summary>
        /// Gets or sets the description/help text that is displayed in the widget tool box.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the default name of the widget.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the widget's view model.
        /// </summary>
        public Type ViewModelType { get; set; }

        /// <summary>
        /// Gets or sets the type of the widget's view.
        /// </summary>
        public Type ViewType { get; set; }
    }
}