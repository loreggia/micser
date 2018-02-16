using System;

namespace Micser.Infrastructure.Widgets
{
    public class ConnectorViewModel : ViewModel
    {
        private ConnectionViewModel _connection;
        private Guid _id;

        public ConnectorViewModel(WidgetViewModel widget, object data)
        {
            Widget = widget;
            Data = data;
            Id = Guid.NewGuid();
        }

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

        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public WidgetViewModel Widget { get; }

        protected virtual void OnConnectionChanged(ConnectionChangedEventArgs e)
        {
            ConnectionChanged?.Invoke(this, e);
        }
    }
}