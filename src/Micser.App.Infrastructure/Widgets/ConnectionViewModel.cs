namespace Micser.App.Infrastructure.Widgets
{
    public class ConnectionViewModel : ViewModel
    {
        private long _id;
        private ConnectorViewModel _source;
        private ConnectorViewModel _target;

        public event ConnectorChangedEventHandler SourceChanged;

        public event ConnectorChangedEventHandler TargetChanged;

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
                    OnTargetChanged(new ConnectorChangedEventArgs(oldValue, value));
                }
            }
        }

        protected virtual void OnSourceChanged(ConnectorChangedEventArgs e)
        {
            SourceChanged?.Invoke(this, e);
        }

        protected virtual void OnTargetChanged(ConnectorChangedEventArgs e)
        {
            TargetChanged?.Invoke(this, e);
        }
    }
}