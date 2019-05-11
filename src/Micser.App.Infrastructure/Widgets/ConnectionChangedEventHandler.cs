using System;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Delegate describing a handler for a connection change event.
    /// </summary>
    public delegate void ConnectionChangedEventHandler(object sender, ConnectionChangedEventArgs e);

    /// <summary>
    /// Event arguments containing data representing the change of a connection on a connector.
    /// </summary>
    public class ConnectionChangedEventArgs : EventArgs
    {
        /// <inheritdoc />
        public ConnectionChangedEventArgs(ConnectionViewModel oldConnection, ConnectionViewModel newConnection)
        {
            OldConnection = oldConnection;
            NewConnection = newConnection;
        }

        /// <summary>
        /// Gets the old connection.
        /// </summary>
        public ConnectionViewModel NewConnection { get; }

        /// <summary>
        /// Gets the new connection.
        /// </summary>
        public ConnectionViewModel OldConnection { get; }
    }
}