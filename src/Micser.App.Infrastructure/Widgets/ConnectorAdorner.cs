using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Widgets
{
    public class ConnectorAdorner : Adorner
    {
        private readonly Pen _drawingPen;
        private readonly Connector _sourceConnector;
        private readonly WidgetPanel _widgetPanel;
        private Widget _hitWidget;
        private PathGeometry _pathGeometry;

        public ConnectorAdorner(WidgetPanel panel, Connector sourceConnector)
            : base(panel)
        {
            _widgetPanel = panel;
            _sourceConnector = sourceConnector;
            _drawingPen = new Pen(Brushes.LightSlateGray, 1)
            {
                LineJoin = PenLineJoin.Round
            };
            Cursor = Cursors.Cross;
        }

        private Connector HitConnector { get; set; }

        private Widget HitWidget
        {
            get => _hitWidget;
            set
            {
                if (_hitWidget != null)
                {
                    _hitWidget.IsDragConnectionOver = false;
                }

                _hitWidget = value;

                if (_hitWidget != null)
                {
                    _hitWidget.IsDragConnectionOver = true;
                }
            }
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
            else
            {
                if (IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (HitConnector != null)
            {
                var sourceConnector = _sourceConnector;
                var sinkConnector = HitConnector;

                _widgetPanel.CreateConnection(sourceConnector, sinkConnector);
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

        private PathGeometry GetPathGeometry(Point position)
        {
            var geometry = new PathGeometry();

            var targetOrientation = HitConnector?.Orientation ?? ConnectorOrientation.None;

            var pathPoints = PathFinder.GetConnectionLine(_sourceConnector.GetInfo(), position, targetOrientation);

            if (pathPoints.Count > 0)
            {
                var figure = new PathFigure
                {
                    StartPoint = pathPoints[0]
                };
                pathPoints.Remove(pathPoints[0]);
                figure.Segments.Add(new PolyLineSegment(pathPoints, true));
                geometry.Figures.Add(figure);
            }

            return geometry;
        }

        private void HitTesting(Point hitPoint)
        {
            var hitConnectorFlag = false;

            var hitObject = _widgetPanel.InputHitTest(hitPoint) as DependencyObject;
            while (hitObject != null &&
                   !Equals(hitObject, _sourceConnector.ParentWidget) &&
                   hitObject.GetType() != typeof(WidgetPanel))
            {
                if (hitObject is Connector connector)
                {
                    HitConnector = connector;
                    hitConnectorFlag = true;
                }

                if (hitObject is Widget widget)
                {
                    HitWidget = widget;
                    if (!hitConnectorFlag)
                    {
                        HitConnector = null;
                    }

                    return;
                }
                hitObject = VisualTreeHelper.GetParent(hitObject);
            }

            HitConnector = null;
            HitWidget = null;
        }
    }
}