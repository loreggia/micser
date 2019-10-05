using Micser.App.Infrastructure.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Represents a connector (input/output) on a widget.
    /// </summary>
    public class Connector : Control
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="IsConnectionSource"/> property.
        /// </summary>
        public static readonly DependencyProperty IsConnectionSourceProperty = DependencyProperty.Register(
            nameof(IsConnectionSource), typeof(bool), typeof(Connector), new PropertyMetadata(true));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation), typeof(ConnectorOrientation), typeof(Connector), new PropertyMetadata(default(ConnectorOrientation)));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Position"/> property.
        /// </summary>
        public static readonly DependencyProperty PositionProperty;

        /// <summary>
        /// Property key for setting the <see cref="PositionProperty"/> dependency property.
        /// </summary>
        protected internal static readonly DependencyPropertyKey PositionPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Position), typeof(Point), typeof(Connector), new PropertyMetadata(default(Point)));

        private Point? _dragStartPoint;

        static Connector()
        {
            PositionProperty = PositionPropertyKey.DependencyProperty;
        }

        /// <inheritdoc />
        public Connector()
        {
            LayoutUpdated += Connector_LayoutUpdated;
        }

        /// <summary>
        /// Gets or sets whether the connector allows being the source of a connection.
        /// Wraps the <see cref="IsConnectionSourceProperty"/> dependency property.
        /// </summary>
        public bool IsConnectionSource
        {
            get => (bool)GetValue(IsConnectionSourceProperty);
            set => SetValue(IsConnectionSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the connector's orientation relative to the widget.
        /// Wraps the <see cref="OrientationProperty"/> dependency property.
        /// </summary>
        public ConnectorOrientation Orientation
        {
            get => (ConnectorOrientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets the Widget this Connector belongs to.
        /// </summary>
        public Widget ParentWidget => this.GetParentOfType<Widget>();

        /// <summary>
        /// Gets the center position of this Connector relative to the WidgetPanel.
        /// </summary>
        public Point Position
        {
            get => (Point)GetValue(PositionProperty);
            protected set => SetValue(PositionPropertyKey, value);
        }

        /// <summary>
        /// Gets the parent <see cref="WidgetPanel"/>.
        /// </summary>
        protected WidgetPanel ParentPanel => this.GetParentOfType<WidgetPanel>();

        internal ConnectorInfo GetInfo()
        {
            var info = new ConnectorInfo
            {
                ParentLeft = Canvas.GetLeft(ParentWidget),
                ParentTop = Canvas.GetTop(ParentWidget),
                ParentSize = new Size(ParentWidget.ActualWidth, ParentWidget.ActualHeight),
                Orientation = Orientation,
                Position = Position
            };

            if (double.IsNaN(info.ParentLeft))
            {
                info.ParentLeft = 0;
            }
            if (double.IsNaN(info.ParentTop))
            {
                info.ParentTop = 0;
            }

            return info;
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (!IsConnectionSource)
            {
                return;
            }

            var panel = ParentPanel;
            if (panel != null)
            {
                // position relative to WidgetPanel
                _dragStartPoint = e.GetPosition(panel);
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // if mouse button is not pressed we have no drag operation, ...
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _dragStartPoint = null;
            }

            // but if mouse button is pressed and start point value is set we do have one
            if (_dragStartPoint.HasValue)
            {
                // create connection adorner
                var panel = ParentPanel;
                if (panel != null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(panel);
                    if (adornerLayer != null)
                    {
                        var adorner = new ConnectorAdorner(panel, this);
                        adornerLayer.Add(adorner);
                        e.Handled = true;
                    }
                }
            }
        }

        private void Connector_LayoutUpdated(object sender, EventArgs e)
        {
            // update the position when the layout changes
            var panel = ParentPanel;
            if (panel != null)
            {
                Position = TransformToAncestor(panel).Transform(new Point(Width / 2, Height / 2));
            }
        }
    }
}