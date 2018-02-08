using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.Main.Controls
{
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
        public double DesignerItemLeft { get; set; }
        public Size DesignerItemSize { get; set; }
        public double DesignerItemTop { get; set; }
        public ConnectorOrientation Orientation { get; set; }
        public Point Position { get; set; }
    }

    public class Connector : Control
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation), typeof(ConnectorOrientation), typeof(Connector), new PropertyMetadata(ConnectorOrientation.None));

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position), typeof(Point), typeof(Connector), new PropertyMetadata(default(Point)));

        private Point? _dragStartPoint;

        public Connector()
        {
            Connections = new List<Connection>();
            LayoutUpdated += OnLayoutUpdated;
        }

        public IList<Connection> Connections { get; }

        public ConnectorOrientation Orientation
        {
            get => (ConnectorOrientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public Widget ParentWidget => DataContext as Widget;

        public Point Position
        {
            get => (Point)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public ConnectorInfo GetInfo()
        {
            var info = new ConnectorInfo
            {
                DesignerItemLeft = Canvas.GetLeft(ParentWidget),
                DesignerItemTop = Canvas.GetTop(ParentWidget),
                DesignerItemSize = new Size(ParentWidget.ActualWidth, ParentWidget.ActualHeight),
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
                // position relative to DesignerCanvas
                _dragStartPoint = e.GetPosition(canvas);
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _dragStartPoint = null;
            }

            // but if mouse button is pressed and start point value is set we do have one
            if (_dragStartPoint.HasValue)
            {
                // create connection adorner
                var panel = GetWidgetPanel(this);
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

        private WidgetPanel GetWidgetPanel(DependencyObject element)
        {
            while (element != null && !(element is WidgetPanel))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            return element as WidgetPanel;
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            var designer = GetWidgetPanel(this);
            if (designer != null)
            {
                //get centre position of this Connector relative to the DesignerCanvas
                Position = TransformToAncestor(designer).Transform(new Point(Width / 2d, Height / 2d));
            }
        }
    }
}