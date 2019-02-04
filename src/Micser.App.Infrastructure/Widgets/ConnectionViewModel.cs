namespace Micser.App.Infrastructure.Widgets
{
    public class ConnectionViewModel : ViewModel
    {
        private long _id;
        private ConnectorViewModel _source;
        private ConnectorViewModel _target;

        public event ConnectorChangedEventHandler SinkChanged;

        public event ConnectorChangedEventHandler SourceChanged;

        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
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

        public ConnectorViewModel Target
        {
            get => _target;
            set
            {
                var oldValue = _target;
                if (SetProperty(ref _target, value))
                {
                    OnSinkChanged(new ConnectorChangedEventArgs(oldValue, value));
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