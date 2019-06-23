namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Represents a widget connector (input/output).
    /// </summary>
    public class ConnectorViewModel : ViewModel
    {
        /// <inheritdoc />
        public ConnectorViewModel(string name, WidgetViewModel widget)
        {
            Name = name;
            Widget = widget;
        }

        /// <summary>
        /// Gets the connector's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the connector's parent widget.
        /// </summary>
        public WidgetViewModel Widget { get; }
    }
}