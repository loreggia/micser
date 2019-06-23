using Micser.App.Infrastructure.Extensions;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// A connection line between two <see cref="Connector"/> controls.
    /// </summary>
    public class Connection : Control, ISelectable
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(Connection), new PropertyMetadata(false, OnIsSelectedPropertyChanged));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="LabelPosition"/> property.
        /// </summary>
        public static readonly DependencyProperty LabelPositionProperty;

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Label"/> property.
        /// </summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(Connection), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="PathGeometry"/> property.
        /// </summary>
        public static readonly DependencyProperty PathGeometryProperty = DependencyProperty.Register(
            nameof(PathGeometry), typeof(PathGeometry), typeof(Connection), new PropertyMetadata(null, OnPathGeometryPropertyChanged));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="SourceAnchorAngle"/> property.
        /// </summary>
        public static readonly DependencyProperty SourceAnchorAngleProperty = DependencyProperty.Register(
            nameof(SourceAnchorAngle), typeof(double), typeof(Connection), new PropertyMetadata(0d));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="SourceAnchorPosition"/> property.
        /// </summary>
        public static readonly DependencyProperty SourceAnchorPositionProperty = DependencyProperty.Register(
            nameof(SourceAnchorPosition), typeof(Point), typeof(Connection), new PropertyMetadata(default(Point)));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Source"/> property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(Connector), typeof(Connection), new PropertyMetadata(null, OnConnectorPropertyChanged));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="StrokeDashArray"/> property.
        /// </summary>
        public static readonly DependencyProperty StrokeDashArrayProperty = DependencyProperty.Register(
            nameof(StrokeDashArray), typeof(DoubleCollection), typeof(Connection), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="TargetAnchorAngle"/> property.
        /// </summary>
        public static readonly DependencyProperty TargetAnchorAngleProperty = DependencyProperty.Register(
            nameof(TargetAnchorAngle), typeof(double), typeof(Connection), new PropertyMetadata(0d));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="TargetAnchorPosition"/> property.
        /// </summary>
        public static readonly DependencyProperty TargetAnchorPositionProperty = DependencyProperty.Register(
            nameof(TargetAnchorPosition), typeof(Point), typeof(Connection), new PropertyMetadata(default(Point)));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="Target"/> property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
            nameof(Target), typeof(Connector), typeof(Connection), new PropertyMetadata(null, OnConnectorPropertyChanged));

        /// <summary>
        /// Property key for the <see cref="LabelPositionProperty"/> dependency property.
        /// </summary>
        protected internal static readonly DependencyPropertyKey LabelPositionPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(LabelPosition), typeof(Point), typeof(Connection), new PropertyMetadata(default(Point)));

        private Adorner _connectionAdorner;

        private WidgetPanel _parentPanel;

        static Connection()
        {
            LabelPositionProperty = LabelPositionPropertyKey.DependencyProperty;
        }

        /// <inheritdoc />
        public Connection(Connector source, Connector target)
        {
            Source = source;
            Target = target;
        }

        /// <inheritdoc />
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// Gets or sets the label text.
        /// Wraps the <see cref="LabelProperty"/> dependency property.
        /// </summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Gets the position of the text label (read-only).
        /// Wraps the <see cref="LabelPositionProperty"/> dependency property.
        /// </summary>
        public Point LabelPosition
        {
            get => (Point)GetValue(LabelPositionProperty);
            private set => SetValue(LabelPositionPropertyKey, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Windows.Media.PathGeometry"/> making up the connection line.
        /// Wraps the <see cref="PathGeometryProperty"/> dependency property.
        /// </summary>
        public PathGeometry PathGeometry
        {
            get => (PathGeometry)GetValue(PathGeometryProperty);
            set => SetValue(PathGeometryProperty, value);
        }

        /// <summary>
        /// Gets or sets the source <see cref="Connector"/>.
        /// Wraps the <see cref="SourceProperty"/> dependency property.
        /// </summary>
        public Connector Source
        {
            get => (Connector)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the angle at which the connection starts from the source connector used for rotating the source anchor.
        /// Wraps the <see cref="SourceAnchorAngleProperty"/> dependency property.
        /// </summary>
        public double SourceAnchorAngle
        {
            get => (double)GetValue(SourceAnchorAngleProperty);
            set => SetValue(SourceAnchorAngleProperty, value);
        }

        /// <summary>
        /// Gets or sets the position offset relative to the source connector where the source anchor is displayed.
        /// Wraps the <see cref="SourceAnchorPositionProperty"/> dependency property.
        /// </summary>
        public Point SourceAnchorPosition
        {
            get => (Point)GetValue(SourceAnchorPositionProperty);
            set => SetValue(SourceAnchorPositionProperty, value);
        }

        /// <summary>
        /// A collection of <see cref="double"/> values that describe the stroke dash distances.
        /// Wraps the <see cref="StrokeDashArrayProperty"/> dependency property.
        /// </summary>
        public DoubleCollection StrokeDashArray
        {
            get => (DoubleCollection)GetValue(StrokeDashArrayProperty);
            set => SetValue(StrokeDashArrayProperty, value);
        }

        /// <summary>
        /// Gets or sets the target connector.
        /// Wraps the <see cref="TargetProperty"/> dependency property.
        /// </summary>
        public Connector Target
        {
            get => (Connector)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// Gets or sets the anchor at which the connection comes into the target connector used for rotating the target anchor.
        /// Wraps the <see cref="TargetAnchorAngleProperty"/> dependency property.
        /// </summary>
        public double TargetAnchorAngle
        {
            get => (double)GetValue(TargetAnchorAngleProperty);
            set => SetValue(TargetAnchorAngleProperty, value);
        }

        /// <summary>
        /// Gets or sets the position offset relative to the target connector where the target anchor is displayed.
        /// Wraps the <see cref="TargetAnchorPositionProperty"/> dependency property.
        /// </summary>
        public Point TargetAnchorPosition
        {
            get => (Point)GetValue(TargetAnchorPositionProperty);
            set => SetValue(TargetAnchorPositionProperty, value);
        }

        private WidgetPanel ParentPanel => _parentPanel ?? (_parentPanel = this.GetParentOfType<WidgetPanel>());

        internal void HideAdorner()
        {
            if (_connectionAdorner != null)
            {
                _connectionAdorner.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Handles selection.
        /// </summary>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            // usual selection business
            var panel = ParentPanel;
            if (panel != null)
            {
                if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None)
                {
                    if (IsSelected)
                    {
                        IsSelected = false;
                    }
                    else
                    {
                        IsSelected = true;
                    }
                }
                else if (!IsSelected)
                {
                    panel.ClearSelection();
                    IsSelected = true;
                }
            }

            //e.Handled = false;
        }

        private static void OnConnectorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var connection = (Connection)d;
            var positionPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(Connector.PositionProperty, typeof(Connector));

            if (e.OldValue is Connector oldConnector)
            {
                positionPropertyDescriptor.RemoveValueChanged(oldConnector, connection.OnConnectorPositionChanged);
            }

            if (e.NewValue is Connector newConnector)
            {
                positionPropertyDescriptor.AddValueChanged(newConnector, connection.OnConnectorPositionChanged);
            }

            connection.UpdatePathGeometry();
        }

        private static void OnIsSelectedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var connection = (Connection)d;
            if (e.NewValue as bool? == true)
            {
                connection.ShowAdorner();
            }
            else
            {
                connection.HideAdorner();
            }
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

        private void ShowAdorner()
        {
            // the ConnectionAdorner is created once for each Connection
            if (_connectionAdorner == null)
            {
                var panel = ParentPanel;
                if (panel != null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(panel);
                    if (adornerLayer != null)
                    {
                        _connectionAdorner = new ConnectionAdorner(panel, this);
                        adornerLayer.Add(_connectionAdorner);
                    }
                }
            }

            if (_connectionAdorner != null)
            {
                _connectionAdorner.Visibility = Visibility.Visible;
            }
        }

        private void UpdateAnchorPosition()
        {
            // the PathGeometry.GetPointAtFractionLength method gets the point and a tangent vector
            // on PathGeometry at the specified fraction of its length
            PathGeometry.GetPointAtFractionLength(0, out var pathStartPoint, out var pathTangentAtStartPoint);
            PathGeometry.GetPointAtFractionLength(1, out var pathEndPoint, out var pathTangentAtEndPoint);
            PathGeometry.GetPointAtFractionLength(0.5, out var pathMidPoint, out _);

            // get angle from tangent vector
            SourceAnchorAngle = Math.Atan2(-pathTangentAtStartPoint.Y, -pathTangentAtStartPoint.X) * (180 / Math.PI);
            TargetAnchorAngle = Math.Atan2(pathTangentAtEndPoint.Y, pathTangentAtEndPoint.X) * (180 / Math.PI);

            // add some margin on source and target side for visual reasons only
            pathStartPoint.Offset(-pathTangentAtStartPoint.X * 5, -pathTangentAtStartPoint.Y * 5);
            pathEndPoint.Offset(pathTangentAtEndPoint.X * 5, pathTangentAtEndPoint.Y * 5);

            SourceAnchorPosition = pathStartPoint;
            TargetAnchorPosition = pathEndPoint;
            LabelPosition = pathMidPoint;
        }

        private void UpdatePathGeometry()
        {
            if (Source != null && Target != null)
            {
                var geometry = new PathGeometry();
                var sourceInfo = Source.GetInfo();
                var targetInfo = Target.GetInfo();
                var linePoints = PathFinder.GetConnectionLine(sourceInfo, targetInfo, true);
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