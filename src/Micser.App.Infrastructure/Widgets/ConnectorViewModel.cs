namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Represents a widget connector (input/output).
    /// </summary>
    public class ConnectorViewModel : ViewModel
    {
        private ConnectionViewModel _connection;

        /// <inheritdoc />
        public ConnectorViewModel(string name, WidgetViewModel widget, object data)
        {
            Name = name;
            Widget = widget;
            Data = data;
        }

        /// <summary>
        /// Raised when the a new connection is made to or from this connector.
        /// </summary>
        public event ConnectionChangedEventHandler ConnectionChanged;

        public ConnectionViewModel Connection
        {
            get => _connection;
            set
            {
                var oldValue = _connection;
                if (SetProperty(ref _connection, value))
                {
                    OnConnectionChanged(new ConnectionChangedEventArgs(oldValue, value));
                }
            }
        }

        public object Data { get; }
        public string Name { get; }
        public WidgetViewModel Widget { get; }

        protected virtual void OnConnectionChanged(ConnectionChangedEventArgs e)
        {
            ConnectionChanged?.Invoke(this, e);
        }
    }
}