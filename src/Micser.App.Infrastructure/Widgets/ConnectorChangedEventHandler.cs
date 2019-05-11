using System;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Delegate describing an event when a connection's source or target connector changes.
    /// </summary>
    public delegate void ConnectorChangedEventHandler(object sender, ConnectorChangedEventArgs e);

    /// <summary>
    /// Contains data describing the change of a connection's connector.
    /// </summary>
    public class ConnectorChangedEventArgs : EventArgs
    {
        /// <inheritdoc />
        public ConnectorChangedEventArgs(ConnectorViewModel oldConnector, ConnectorViewModel newConnector)
        {
            OldConnector = oldConnector;
            NewConnector = newConnector;
        }

        /// <summary>
        /// Gets the old connector.
        /// </summary>
        public ConnectorViewModel NewConnector { get; }

        /// <summary>
        /// Gets the new connector.
        /// </summary>
        public ConnectorViewModel OldConnector { get; }
    }
}