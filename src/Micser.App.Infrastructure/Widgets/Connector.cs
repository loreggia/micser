using Micser.App.Infrastructure.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Widgets
{
    public class Connector : Control
    {
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register(
            nameof(Connection), typeof(Connection), typeof(Connector), new PropertyMetadata(null));

        public static readonly DependencyProperty IsConnectionSourceProperty = DependencyProperty.Register(
            nameof(IsConnectionSource), typeof(bool), typeof(Connector), new PropertyMetadata(true));

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position), typeof(Point), typeof(Connector), new PropertyMetadata(default(Point)));

        private Point? _dragStartPoint;

        private WidgetPanel _parentPanel;
        private Widget _parentWidget;

        public Connector()
        {
            LayoutUpdated += Connector_LayoutUpdated;
            Loaded += Connector_Loaded;
        }

        public Connection Connection
        {
            get => (Connection)GetValue(ConnectionProperty);
            set => SetValue(ConnectionProperty, value);
        }

        public bool IsConnectionSource
        {
            get => (bool)GetValue(IsConnectionSourceProperty);
            set => SetValue(IsConnectionSourceProperty, value);
        }

        public ConnectorOrientation Orientation { get; set; }

        /// <summary>
        /// The Widget this Connector belongs to.
        /// </summary>
        public Widget ParentWidget => _parentWidget ?? (_parentWidget = this.GetParentOfType<Widget>());

        /// <summary>
        /// Center position of this Connector relative to the WidgetPanel.
        /// </summary>
        public Point Position
        {
            get => (Point)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        private WidgetPanel ParentPanel => _parentPanel ?? (_parentPanel = this.GetParentOfType<WidgetPanel>());

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
            return info;
        }

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

        // when the layout changes we update the position property
        private void Connector_LayoutUpdated(object sender, EventArgs e)
        {
            var panel = ParentPanel;
            if (panel != null)
            {
                //get centre position of this Connector relative to the WidgetPanel
                Position = TransformToAncestor(panel).Transform(new Point(Width / 2, Height / 2));
            }
        }

        private void Connector_Loaded(object sender, RoutedEventArgs e)
        {
            _parentPanel.UpdateConnections();
        }
    }
}