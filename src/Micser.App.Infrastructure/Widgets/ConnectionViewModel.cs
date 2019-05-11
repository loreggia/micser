namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// A widget connection between two connectors.
    /// </summary>
    public class ConnectionViewModel : ViewModel
    {
        private long _id;
        private ConnectorViewModel _source;
        private ConnectorViewModel _target;

        /// <summary>
        /// Event raised when the source of the connection has changed.
        /// </summary>
        public event ConnectorChangedEventHandler SourceChanged;

        /// <summary>
        /// Event raised when the target of the connection has changed.
        /// </summary>
        public event ConnectorChangedEventHandler TargetChanged;

        /// <summary>
        /// Gets or sets the connection's persistence ID.
        /// </summary>
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets or sets the source connector.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the target connector.
        /// </summary>
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

        /// <summary>
        /// Raises the <see cref="SourceChanged"/> event.
        /// </summary>
        protected virtual void OnSourceChanged(ConnectorChangedEventArgs e)
        {
            SourceChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="TargetChanged"/> event.
        /// </summary>
        protected virtual void OnTargetChanged(ConnectorChangedEventArgs e)
        {
            TargetChanged?.Invoke(this, e);
        }
    }
}