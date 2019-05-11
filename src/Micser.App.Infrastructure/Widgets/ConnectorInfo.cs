using System.Windows;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// The orientation of a connector that is used for displaying a connection line from the correct starting point.
    /// </summary>
    public enum ConnectorOrientation
    {
        None,
        Left,
        Top,
        Right,
        Bottom
    }

    /// <summary>
    /// Provides compact info about a connector; used for the routing algorithm, instead of hand over a full fledged Connector.
    /// </summary>
    public struct ConnectorInfo
    {
        public ConnectorOrientation Orientation { get; set; }
        public double ParentLeft { get; set; }
        public Size ParentSize { get; set; }
        public double ParentTop { get; set; }
        public Point Position { get; set; }
    }
}