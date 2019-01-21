using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Widgets
{
    public class ConnectionAdorner : Adorner
    {
        private readonly Canvas _adornerCanvas;
        private readonly Connection _connection;
        private readonly Pen _drawingPen;
        private readonly VisualCollection _visualChildren;
        private readonly WidgetPanel _widgetPanel;
        private Connector _fixConnector, _dragConnector;
        private Widget _hitWidget;
        private PathGeometry _pathGeometry;
        private Thumb _sourceDragThumb, _sinkDragThumb;

        public ConnectionAdorner(WidgetPanel panel, Connection connection)
            : base(panel)
        {
            _widgetPanel = panel;
            _adornerCanvas = new Canvas();
            _visualChildren = new VisualCollection(this)
            {
                _adornerCanvas
            };

            _connection = connection;

            var spd = DependencyPropertyDescriptor.FromProperty(Connection.SourceAnchorPositionProperty, typeof(Connection));
            spd.AddValueChanged(_connection, SourceAnchorPositionChanged);

            spd = DependencyPropertyDescriptor.FromProperty(Connection.SinkAnchorPositionProperty, typeof(Connection));
            spd.AddValueChanged(_connection, SinkAnchorPositionChanged);

            InitializeDragThumbs();

            _drawingPen = new Pen(Brushes.LightSlateGray, 1)
            {
                LineJoin = PenLineJoin.Round
            };
        }

        protected override int VisualChildrenCount => _visualChildren.Count;

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

        protected override Size ArrangeOverride(Size finalSize)
        {
            _adornerCanvas.Arrange(new Rect(0, 0, _widgetPanel.ActualWidth, _widgetPanel.ActualHeight));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visualChildren[index];
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            dc.DrawGeometry(null, _drawingPen, _pathGeometry);
        }

        private void DragThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (HitConnector != null)
            {
                var connectionVm = (ConnectionViewModel) _connection.DataContext;
                var connectorVm = (ConnectorViewModel) HitConnector.DataContext;

                if (Equals(_connection.Source, _fixConnector))
                {
                    _connection.Sink = HitConnector;
                    connectionVm.Sink = connectorVm;
                }
                else
                {
                    _connection.Source = HitConnector;
                    connectionVm.Source = connectorVm;
                }

                connectorVm.Connection = connectionVm;
            }
            else
            {
                // remove connection
                _connection.Sink.Connection = null;
                _connection.Source.Connection = null;
                _connection.Sink = null;
                _connection.Source = null;
                _widgetPanel.RemoveConnection(_connection);
            }

            HitWidget = null;
            HitConnector = null;
            _pathGeometry = null;
            _connection.StrokeDashArray = null;
            InvalidateVisual();
        }

        private void DragThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var currentPosition = Mouse.GetPosition(this);
            HitTesting(currentPosition);
            _pathGeometry = UpdatePathGeometry(currentPosition);
            InvalidateVisual();
        }

        private void DragThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            HitWidget = null;
            HitConnector = null;
            _pathGeometry = null;
            Cursor = Cursors.Cross;
            _connection.StrokeDashArray = new DoubleCollection(new double[] {1, 2});

            if (Equals(sender, _sourceDragThumb))
            {
                _fixConnector = _connection.Sink;
                _dragConnector = _connection.Source;
            }
            else if (Equals(sender, _sinkDragThumb))
            {
                _dragConnector = _connection.Sink;
                _fixConnector = _connection.Source;
            }
        }

        private void HitTesting(Point hitPoint)
        {
            var hitConnectorFlag = false;

            var hitObject = _widgetPanel.InputHitTest(hitPoint) as DependencyObject;
            while (hitObject != null &&
                   !Equals(hitObject, _fixConnector.ParentWidget) &&
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

        private void InitializeDragThumbs()
        {
            var dragThumbStyle = _connection.FindResource("ConnectionAdornerThumbStyle") as Style;

            //source drag thumb
            _sourceDragThumb = new Thumb();
            Canvas.SetLeft(_sourceDragThumb, _connection.SourceAnchorPosition.X);
            Canvas.SetTop(_sourceDragThumb, _connection.SourceAnchorPosition.Y);
            _adornerCanvas.Children.Add(_sourceDragThumb);
            if (dragThumbStyle != null)
            {
                _sourceDragThumb.Style = dragThumbStyle;
            }

            _sourceDragThumb.DragDelta += DragThumb_DragDelta;
            _sourceDragThumb.DragStarted += DragThumb_DragStarted;
            _sourceDragThumb.DragCompleted += DragThumb_DragCompleted;

            // sink drag thumb
            _sinkDragThumb = new Thumb();
            Canvas.SetLeft(_sinkDragThumb, _connection.SinkAnchorPosition.X);
            Canvas.SetTop(_sinkDragThumb, _connection.SinkAnchorPosition.Y);
            _adornerCanvas.Children.Add(_sinkDragThumb);
            if (dragThumbStyle != null)
            {
                _sinkDragThumb.Style = dragThumbStyle;
            }

            _sinkDragThumb.DragDelta += DragThumb_DragDelta;
            _sinkDragThumb.DragStarted += DragThumb_DragStarted;
            _sinkDragThumb.DragCompleted += DragThumb_DragCompleted;
        }

        private void SinkAnchorPositionChanged(object sender, EventArgs e)
        {
            Canvas.SetLeft(_sinkDragThumb, _connection.SinkAnchorPosition.X);
            Canvas.SetTop(_sinkDragThumb, _connection.SinkAnchorPosition.Y);
        }

        private void SourceAnchorPositionChanged(object sender, EventArgs e)
        {
            Canvas.SetLeft(_sourceDragThumb, _connection.SourceAnchorPosition.X);
            Canvas.SetTop(_sourceDragThumb, _connection.SourceAnchorPosition.Y);
        }

        private PathGeometry UpdatePathGeometry(Point position)
        {
            var geometry = new PathGeometry();

            var targetOrientation = HitConnector?.Orientation ?? _dragConnector.Orientation;

            var linePoints = PathFinder.GetConnectionLine(_fixConnector.GetInfo(), position, targetOrientation);

            if (linePoints.Count > 0)
            {
                var figure = new PathFigure
                {
                    StartPoint = linePoints[0]
                };
                linePoints.Remove(linePoints[0]);
                figure.Segments.Add(new PolyLineSegment(linePoints, true));
                geometry.Figures.Add(figure);
            }

            return geometry;
        }
    }
}