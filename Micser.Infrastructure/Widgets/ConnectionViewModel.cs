using System;

namespace Micser.Infrastructure.Widgets
{
    public class ConnectionViewModel : ViewModel
    {
        private Guid _id;
        private ConnectorViewModel _sink;
        private ConnectorViewModel _source;

        public ConnectionViewModel()
        {
            Id = Guid.NewGuid();
        }

        public event ConnectorChangedEventHandler SinkChanged;

        public event ConnectorChangedEventHandler SourceChanged;

        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public ConnectorViewModel Sink
        {
            get => _sink;
            set
            {
                var oldValue = _sink;
                if (SetProperty(ref _sink, value))
                {
                    OnSinkChanged(new ConnectorChangedEventArgs(oldValue, value));
                }
            }
        }

        public ConnectorViewModel Source
        {
            get => _source;
            set
            {
                var oldValue = _source;
                if (SetProperty(ref _source, value))
                {
                    OnSourceChanged(new ConnectorChangedEventArgs(oldValue, value));
                }
            }
        }

        protected virtual void OnSinkChanged(ConnectorChangedEventArgs e)
        {
            SinkChanged?.Invoke(this, e);
        }

        protected virtual void OnSourceChanged(ConnectorChangedEventArgs e)
        {
            SourceChanged?.Invoke(this, e);
        }
    }
}