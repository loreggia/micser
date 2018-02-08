using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Micser.Main.Controls
{
    public class Connection : Control, ISelectable
    {
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(Connection), new PropertyMetadata(false));

        public static readonly DependencyProperty PathGeometryProperty;

        public static readonly DependencyProperty SinkProperty = DependencyProperty.Register(
            nameof(Sink), typeof(Connector), typeof(Connection), new PropertyMetadata(null, OnConnectorPropertyChanged));

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(Connector), typeof(Connection), new PropertyMetadata(null, OnConnectorPropertyChanged));

        private static readonly DependencyPropertyKey _pathGeometryPropertyKey;

        static Connection()
        {
            _pathGeometryPropertyKey = DependencyProperty.RegisterReadOnly(
                nameof(PathGeometry), typeof(PathGeometry), typeof(Connection), new PropertyMetadata(null, OnPathGeometryPropertyChanged));
            PathGeometryProperty = _pathGeometryPropertyKey.DependencyProperty;
        }

        public Connection(Connector source, Connector sink)
        {
            Source = source;
            Sink = sink;
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public PathGeometry PathGeometry
        {
            get => (PathGeometry)GetValue(PathGeometryProperty);
            private set => SetValue(_pathGeometryPropertyKey, value);
        }

        public Connector Sink
        {
            get => (Connector)GetValue(SinkProperty);
            set => SetValue(SinkProperty, value);
        }

        public Connector Source
        {
            get => (Connector)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private static void OnConnectorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var connection = (Connection)d;
            var positionPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(Connector.PositionProperty, typeof(Connector));

            if (e.OldValue is Connector oldConnector)
            {
                positionPropertyDescriptor.RemoveValueChanged(oldConnector, connection.OnConnectorPositionChanged);
                oldConnector.Connections.Remove(connection);
            }

            if (e.NewValue is Connector newConnector)
            {
                positionPropertyDescriptor.AddValueChanged(newConnector, connection.OnConnectorPositionChanged);
                newConnector.Connections.Add(connection);
            }

            connection.UpdatePathGeometry();
        }

        private static void OnPathGeometryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var connection = (Connection)d;
            connection.UpdateAnchorPosition();
        }

        private void OnConnectorPositionChanged(object sender, EventArgs e)
        {
            UpdatePathGeometry();
        }

        private void UpdateAnchorPosition()
        {
            Point pathStartPoint, pathTangentAtStartPoint;
            Point pathEndPoint, pathTangentAtEndPoint;
            Point pathMidPoint, pathTangentAtMidPoint;

            // the PathGeometry.GetPointAtFractionLength method gets the point and a tangent vector
            // on PathGeometry at the specified fraction of its length
            PathGeometry.GetPointAtFractionLength(0, out pathStartPoint, out pathTangentAtStartPoint);
            PathGeometry.GetPointAtFractionLength(1, out pathEndPoint, out pathTangentAtEndPoint);
            PathGeometry.GetPointAtFractionLength(0.5, out pathMidPoint, out pathTangentAtMidPoint);

            // get angle from tangent vector
            AnchorAngleSource = Math.Atan2(-pathTangentAtStartPoint.Y, -pathTangentAtStartPoint.X) * (180 / Math.PI);
            AnchorAngleSink = Math.Atan2(pathTangentAtEndPoint.Y, pathTangentAtEndPoint.X) * (180 / Math.PI);

            // add some margin on source and sink side for visual reasons only
            pathStartPoint.Offset(-pathTangentAtStartPoint.X * 5, -pathTangentAtStartPoint.Y * 5);
            pathEndPoint.Offset(pathTangentAtEndPoint.X * 5, pathTangentAtEndPoint.Y * 5);

            AnchorPositionSource = pathStartPoint;
            AnchorPositionSink = pathEndPoint;
            LabelPosition = pathMidPoint;
        }

        private void UpdatePathGeometry()
        {
            if (Source != null && Sink != null)
            {
                var geometry = new PathGeometry();
                var linePoints = PathFinder.GetConnectionLine(Source.GetInfo(), Sink.GetInfo(), true);
                if (linePoints.Count > 0)
                {
                    var figure = new PathFigure
                    {
                        StartPoint = linePoints[0]
                    };
                    linePoints.Remove(linePoints[0]);
                    figure.Segments.Add(new PolyLineSegment(linePoints, true));
                    geometry.Figures.Add(figure);

                    PathGeometry = geometry;
                }
            }
        }
    }
}