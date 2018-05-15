using System;

namespace Micser.Infrastructure.Widgets
{
    public delegate void ConnectionChangedEventHandler(object sender, ConnectionChangedEventArgs e);

    public class ConnectionChangedEventArgs : EventArgs
    {
        public ConnectionChangedEventArgs(ConnectionViewModel oldConnection, ConnectionViewModel newConnection)
        {
            OldConnection = oldConnection;
            NewConnection = newConnection;
        }

        public ConnectionViewModel NewConnection { get; }
        public ConnectionViewModel OldConnection { get; }
    }
}