using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.Main.Controls
{
    public class ConnectorAdorner : Adorner
    {
        public static readonly DependencyProperty HitConnectorProperty = DependencyProperty.Register(
            nameof(HitConnector), typeof(Connector), typeof(ConnectorAdorner), new PropertyMetadata(null));

        public static readonly DependencyProperty HitWidgetProperty = DependencyProperty.Register(
                    nameof(HitWidget), typeof(Widget), typeof(ConnectorAdorner), new PropertyMetadata(null, OnHitWidgetPropertyChanged));

        private readonly Pen _drawingPen;
        private readonly Connector _sourceConnector;
        private readonly WidgetPanel _widgetPanel;
        private PathGeometry _pathGeometry;

        public ConnectorAdorner(WidgetPanel widgetPanel, Connector sourceConnector)
            : base(widgetPanel)
        {
            _widgetPanel = widgetPanel;
            _sourceConnector = sourceConnector;
            _drawingPen = new Pen(Brushes.LightSlateGray, 1);
            _drawingPen.LineJoin = PenLineJoin.Round;

            Cursor = Cursors.Cross;
        }

        public Connector HitConnector
        {
            get => (Connector)GetValue(HitConnectorProperty);
            set => SetValue(HitConnectorProperty, value);
        }

        public Widget HitWidget
        {
            get => (Widget)GetValue(HitWidgetProperty);
            set => SetValue(HitWidgetProperty, value);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!IsMouseCaptured)
                {
                    CaptureMouse();
                }

                HitTesting(e.GetPosition(this));
                _pathGeometry = GetPathGeometry(e.GetPosition(this));
                InvalidateVisual();
            }
            else if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (HitConnector != null)
            {
                var sourceConnector = _sourceConnector;
                var sinkConnector = HitConnector;
                var newConnection = new Connection(sourceConnector, sinkConnector);

                // connections are added with z-index of zero
                _widgetPanel.Children.Insert(0, newConnection);
            }
            if (HitWidget != null)
            {
                HitWidget.IsDragConnectionOver = false;
            }

            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }

            var adornerLayer = AdornerLayer.GetAdornerLayer(_widgetPanel);
            adornerLayer?.Remove(this);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            dc.DrawGeometry(null, _drawingPen, _pathGeometry);

            // without a background the OnMouseMove event would not be fired
            // Alternative: implement a Canvas as a child of this adorner, like
            // the ConnectionAdorner does.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        }

        private static void OnHitWidgetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is Widget oldWidget)
            {
                oldWidget.IsDragConnectionOver = false;
            }

            if (e.NewValue is Widget newWidget)
            {
                newWidget.IsDragConnectionOver = true;
            }
        }

        private PathGeometry GetPathGeometry(Point position)
        {
            var geometry = new PathGeometry();

            ConnectorOrientation targetOrientation;
            if (HitConnector != null)
            {
                targetOrientation = HitConnector.Orientation;
            }
            else
            {
                targetOrientation = ConnectorOrientation.None;
            }

            List<Point> pathPoints = PathFinder.GetConnectionLine(sourceConnector.GetInfo(), position, targetOrientation);

            if (pathPoints.Count > 0)
            {
                PathFigure figure = new PathFigure();
                figure.StartPoint = pathPoints[0];
                pathPoints.Remove(pathPoints[0]);
                figure.Segments.Add(new PolyLineSegment(pathPoints, true));
                geometry.Figures.Add(figure);
            }

            return geometry;
        }

        private void HitTesting(Point hitPoint)
        {
            bool hitConnectorFlag = false;

            DependencyObject hitObject = designerCanvas.InputHitTest(hitPoint) as DependencyObject;
            while (hitObject != null &&
                   hitObject != sourceConnector.ParentDesignerItem &&
                   hitObject.GetType() != typeof(DesignerCanvas))
            {
                if (hitObject is Connector)
                {
                    HitConnector = hitObject as Connector;
                    hitConnectorFlag = true;
                }

                if (hitObject is DesignerItem)
                {
                    HitDesignerItem = hitObject as DesignerItem;
                    if (!hitConnectorFlag)
                        HitConnector = null;
                    return;
                }
                hitObject = VisualTreeHelper.GetParent(hitObject);
            }

            HitConnector = null;
            HitDesignerItem = null;
        }
    }
}