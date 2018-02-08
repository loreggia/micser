using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramDesigner
{
    public class ConnectorAdorner : Adorner
    {
        private readonly Pen _drawingPen;
        private readonly Connector _sourceConnector;
        private readonly WidgetPanel _widgetPanel;
        private Connector _hitConnector;
        private Widget _hitWidget;
        private PathGeometry _pathGeometry;

        public ConnectorAdorner(WidgetPanel panel, Connector sourceConnector)
            : base(panel)
        {
            _widgetPanel = panel;
            _sourceConnector = sourceConnector;
            _drawingPen = new Pen(Brushes.LightSlateGray, 1);
            _drawingPen.LineJoin = PenLineJoin.Round;
            Cursor = Cursors.Cross;
        }

        private Connector HitConnector
        {
            get { return _hitConnector; }
            set
            {
                if (_hitConnector != value)
                {
                    _hitConnector = value;
                }
            }
        }

        private Widget HitWidget
        {
            get { return _hitWidget; }
            set
            {
                if (_hitWidget != value)
                {
                    if (_hitWidget != null)
                        _hitWidget.IsDragConnectionOver = false;

                    _hitWidget = value;

                    if (_hitWidget != null)
                        _hitWidget.IsDragConnectionOver = true;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!this.IsMouseCaptured) this.CaptureMouse();
                HitTesting(e.GetPosition(this));
                this._pathGeometry = GetPathGeometry(e.GetPosition(this));
                this.InvalidateVisual();
            }
            else
            {
                if (this.IsMouseCaptured) this.ReleaseMouseCapture();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (HitConnector != null)
            {
                Connector sourceConnector = this._sourceConnector;
                Connector sinkConnector = this.HitConnector;
                Connection newConnection = new Connection(sourceConnector, sinkConnector);

                // connections are added with z-index of zero
                this._widgetPanel.Children.Insert(0, newConnection);
            }
            if (HitWidget != null)
            {
                this.HitWidget.IsDragConnectionOver = false;
            }

            if (this.IsMouseCaptured) this.ReleaseMouseCapture();

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this._widgetPanel);
            if (adornerLayer != null)
            {
                adornerLayer.Remove(this);
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            dc.DrawGeometry(null, _drawingPen, this._pathGeometry);

            // without a background the OnMouseMove event would not be fired
            // Alternative: implement a Canvas as a child of this adorner, like
            // the ConnectionAdorner does.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        }

        private PathGeometry GetPathGeometry(Point position)
        {
            PathGeometry geometry = new PathGeometry();

            ConnectorOrientation targetOrientation;
            if (HitConnector != null)
                targetOrientation = HitConnector.Orientation;
            else
                targetOrientation = ConnectorOrientation.None;

            List<Point> pathPoints = PathFinder.GetConnectionLine(_sourceConnector.GetInfo(), position, targetOrientation);

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

            DependencyObject hitObject = _widgetPanel.InputHitTest(hitPoint) as DependencyObject;
            while (hitObject != null &&
                   hitObject != _sourceConnector.ParentWidget &&
                   hitObject.GetType() != typeof(WidgetPanel))
            {
                if (hitObject is Connector)
                {
                    HitConnector = hitObject as Connector;
                    hitConnectorFlag = true;
                }

                if (hitObject is Widget)
                {
                    HitWidget = hitObject as Widget;
                    if (!hitConnectorFlag)
                        HitConnector = null;
                    return;
                }
                hitObject = VisualTreeHelper.GetParent(hitObject);
            }

            HitConnector = null;
            HitWidget = null;
        }
    }
}