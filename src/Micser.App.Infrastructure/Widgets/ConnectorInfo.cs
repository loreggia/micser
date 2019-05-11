using System.Windows;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// The orientation of a connector that is used for displaying a connection line from the correct starting point.
    /// </summary>
    public enum ConnectorOrientation
    {
        /// <summary>
        /// No specific orientation. Connection display will start at the center of the connector.
        /// </summary>
        None,

        /// <summary>
        /// The connector is on the left of the parent control.
        /// </summary>
        Left,

        /// <summary>
        /// The connector is on the top of the parent control.
        /// </summary>
        Top,

        /// <summary>
        /// The connector is on the right of the parent control.
        /// </summary>
        Right,

        /// <summary>
        /// The connector is on the bottom of the parent control.
        /// </summary>
        Bottom
    }

    /// <summary>
    /// Provides compact info about a connector; used for the routing algorithm, instead of hand over a full fledged Connector.
    /// </summary>
    public struct ConnectorInfo
    {
        /// <summary>
        /// Gets or sets the connector's orientation.
        /// </summary>
        public ConnectorOrientation Orientation { get; set; }

        /// <summary>
        /// The distance from the left of the parent control.
        /// </summary>
        public double ParentLeft { get; set; }

        /// <summary>
        /// The size of the parent control.
        /// </summary>
        public Size ParentSize { get; set; }

        /// <summary>
        /// The distance from the top of the parent control.
        /// </summary>
        public double ParentTop { get; set; }

        /// <summary>
        /// The center position of the connector relative to the parent control.
        /// </summary>
        public Point Position { get; set; }
    }
}