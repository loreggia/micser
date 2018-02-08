using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramDesigner
{
    public class ConnectionAdorner : Adorner
    {
        private readonly Canvas _adornerCanvas;
        private readonly Connection _connection;
        private readonly Pen _drawingPen;
        private readonly VisualCollection _visualChildren;
        private readonly WidgetPanel _widgetPanel;
        private Connector _fixConnector, _dragConnector;
        private Connector _hitConnector;
        private Widget _hitWidget;
        private PathGeometry _pathGeometry;
        private Thumb _sourceDragThumb, _sinkDragThumb;

        public ConnectionAdorner(WidgetPanel panel, Connection connection)
            : base(panel)
        {
            _widgetPanel = panel;
            _adornerCanvas = new Canvas();
            _visualChildren = new VisualCollection(this);
            _visualChildren.Add(_adornerCanvas);

            _connection = connection;
            _connection.PropertyChanged += new PropertyChangedEventHandler(AnchorPositionChanged);

            InitializeDragThumbs();

            _drawingPen = new Pen(Brushes.LightSlateGray, 1);
            _drawingPen.LineJoin = PenLineJoin.Round;
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return this._visualChildren.Count;
            }
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

        protected override Size ArrangeOverride(Size finalSize)
        {
            _adornerCanvas.Arrange(new Rect(0, 0, this._widgetPanel.ActualWidth, this._widgetPanel.ActualHeight));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return this._visualChildren[index];
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            dc.DrawGeometry(null, _drawingPen, this._pathGeometry);
        }

        private void AnchorPositionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("AnchorPositionSource"))
            {
                Canvas.SetLeft(_sourceDragThumb, _connection.AnchorPositionSource.X);
                Canvas.SetTop(_sourceDragThumb, _connection.AnchorPositionSource.Y);
            }

            if (e.PropertyName.Equals("AnchorPositionSink"))
            {
                Canvas.SetLeft(_sinkDragThumb, _connection.AnchorPositionSink.X);
                Canvas.SetTop(_sinkDragThumb, _connection.AnchorPositionSink.Y);
            }
        }

        private void HitTesting(Point hitPoint)
        {
            bool hitConnectorFlag = false;

            DependencyObject hitObject = _widgetPanel.InputHitTest(hitPoint) as DependencyObject;
            while (hitObject != null &&
                   hitObject != _fixConnector.ParentWidget &&
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

        private void InitializeDragThumbs()
        {
            Style dragThumbStyle = _connection.FindResource("ConnectionAdornerThumbStyle") as Style;

            //source drag thumb
            _sourceDragThumb = new Thumb();
            Canvas.SetLeft(_sourceDragThumb, _connection.AnchorPositionSource.X);
            Canvas.SetTop(_sourceDragThumb, _connection.AnchorPositionSource.Y);
            this._adornerCanvas.Children.Add(_sourceDragThumb);
            if (dragThumbStyle != null)
                _sourceDragThumb.Style = dragThumbStyle;

            _sourceDragThumb.DragDelta += new DragDeltaEventHandler(thumbDragThumb_DragDelta);
            _sourceDragThumb.DragStarted += new DragStartedEventHandler(thumbDragThumb_DragStarted);
            _sourceDragThumb.DragCompleted += new DragCompletedEventHandler(thumbDragThumb_DragCompleted);

            // sink drag thumb
            _sinkDragThumb = new Thumb();
            Canvas.SetLeft(_sinkDragThumb, _connection.AnchorPositionSink.X);
            Canvas.SetTop(_sinkDragThumb, _connection.AnchorPositionSink.Y);
            this._adornerCanvas.Children.Add(_sinkDragThumb);
            if (dragThumbStyle != null)
                _sinkDragThumb.Style = dragThumbStyle;

            _sinkDragThumb.DragDelta += new DragDeltaEventHandler(thumbDragThumb_DragDelta);
            _sinkDragThumb.DragStarted += new DragStartedEventHandler(thumbDragThumb_DragStarted);
            _sinkDragThumb.DragCompleted += new DragCompletedEventHandler(thumbDragThumb_DragCompleted);
        }

        private void thumbDragThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (HitConnector != null)
            {
                if (_connection != null)
                {
                    if (_connection.Source == _fixConnector)
                        _connection.Sink = this.HitConnector;
                    else
                        _connection.Source = this.HitConnector;
                }
            }

            this.HitWidget = null;
            this.HitConnector = null;
            this._pathGeometry = null;
            this._connection.StrokeDashArray = null;
            this.InvalidateVisual();
        }

        private void thumbDragThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Point currentPosition = Mouse.GetPosition(this);
            this.HitTesting(currentPosition);
            this._pathGeometry = UpdatePathGeometry(currentPosition);
            this.InvalidateVisual();
        }

        private void thumbDragThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            this.HitWidget = null;
            this.HitConnector = null;
            this._pathGeometry = null;
            this.Cursor = Cursors.Cross;
            this._connection.StrokeDashArray = new DoubleCollection(new double[] { 1, 2 });

            if (sender == _sourceDragThumb)
            {
                _fixConnector = _connection.Sink;
                _dragConnector = _connection.Source;
            }
            else if (sender == _sinkDragThumb)
            {
                _dragConnector = _connection.Sink;
                _fixConnector = _connection.Source;
            }
        }

        private PathGeometry UpdatePathGeometry(Point position)
        {
            PathGeometry geometry = new PathGeometry();

            ConnectorOrientation targetOrientation;
            if (HitConnector != null)
                targetOrientation = HitConnector.Orientation;
            else
                targetOrientation = _dragConnector.Orientation;

            List<Point> linePoints = PathFinder.GetConnectionLine(_fixConnector.GetInfo(), position, targetOrientation);

            if (linePoints.Count > 0)
            {
                PathFigure figure = new PathFigure();
                figure.StartPoint = linePoints[0];
                linePoints.Remove(linePoints[0]);
                figure.Segments.Add(new PolyLineSegment(linePoints, true));
                geometry.Figures.Add(figure);
            }

            return geometry;
        }
    }
}