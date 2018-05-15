using System;

namespace Micser.Infrastructure.Widgets
{
    public delegate void ConnectorChangedEventHandler(object sender, ConnectorChangedEventArgs e);

    public class ConnectorChangedEventArgs : EventArgs
    {
        public ConnectorChangedEventArgs(ConnectorViewModel oldConnector, ConnectorViewModel newConnector)
        {
            OldConnector = oldConnector;
            NewConnector = newConnector;
        }

        public ConnectorViewModel NewConnector { get; }
        public ConnectorViewModel OldConnector { get; }
    }
}