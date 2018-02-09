using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Micser.Infrastructure;

namespace Micser.Main.Controls
{
    public class Connector : Control
    {
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position), typeof(Point), typeof(Connector), new PropertyMetadata(default(Point)));

        // drag start point, relative to the WidgetPanel
        private Point? _dragStartPoint;

        private Widget _parentWidget;

        public Connector()
        {
            Connections = new List<Connection>();
            LayoutUpdated += Connector_LayoutUpdated;
        }

        public List<Connection> Connections { get; }

        public ConnectorOrientation Orientation { get; set; }

        /// <summary>
        /// The Widget this Connector belongs to; retrieved from DataContext, which is set in the Widget template
        /// </summary>
        public Widget ParentWidget => _parentWidget ?? (_parentWidget = DataContext as Widget);

        /// <summary>
        /// Center position of this Connector relative to the WidgetPanel.
        /// </summary>
        public Point Position
        {
            get => (Point)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

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

            var canvas = GetWidgetPanel(this);
            if (canvas != null)
            {
                // position relative to WidgetPanel
                _dragStartPoint = e.GetPosition(canvas);
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
                var canvas = GetWidgetPanel(this);
                if (canvas != null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(canvas);
                    if (adornerLayer != null)
                    {
                        var adorner = new ConnectorAdorner(canvas, this);
                        adornerLayer.Add(adorner);
                        e.Handled = true;
                    }
                }
            }
        }

        // iterate through visual tree to get parent WidgetPanel
        private static WidgetPanel GetWidgetPanel(DependencyObject element)
        {
            while (element != null && !(element is WidgetPanel))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            return element as WidgetPanel;
        }

        // when the layout changes we update the position property
        private void Connector_LayoutUpdated(object sender, EventArgs e)
        {
            var panel = GetWidgetPanel(this);
            if (panel != null)
            {
                //get centre position of this Connector relative to the WidgetPanel
                Position = TransformToAncestor(panel).Transform(new Point(Width / 2, Height / 2));
            }
        }
    }
}