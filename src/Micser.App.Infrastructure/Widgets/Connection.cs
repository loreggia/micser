using Micser.App.Infrastructure.Extensions;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Widgets
{
    public enum ArrowSymbol
    {
        None,
        Arrow,
        Diamond
    }

    public class Connection : Control, ISelectable
    {
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(Connection), new PropertyMetadata(false, OnIsSelectedPropertyChanged));

        public static readonly DependencyProperty LabelPositionProperty;

        public static readonly DependencyPropertyKey LabelPositionPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(LabelPosition), typeof(Point), typeof(Connection), new PropertyMetadata(default(Point)));

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(Connection), new PropertyMetadata(null));

        public static readonly DependencyProperty PathGeometryProperty = DependencyProperty.Register(
            nameof(PathGeometry), typeof(PathGeometry), typeof(Connection), new PropertyMetadata(null, OnPathGeometryPropertyChanged));

        public static readonly DependencyProperty SourceAnchorAngleProperty = DependencyProperty.Register(
            nameof(SourceAnchorAngle), typeof(double), typeof(Connection), new PropertyMetadata(0d));

        public static readonly DependencyProperty SourceAnchorPositionProperty = DependencyProperty.Register(
            nameof(SourceAnchorPosition), typeof(Point), typeof(Connection), new PropertyMetadata(default(Point)));

        public static readonly DependencyProperty SourceArrowSymbolProperty = DependencyProperty.Register(
            nameof(SourceArrowSymbol), typeof(ArrowSymbol), typeof(Connection), new PropertyMetadata(ArrowSymbol.None));

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(Connector), typeof(Connection), new PropertyMetadata(null, OnConnectorPropertyChanged));

        public static readonly DependencyProperty StrokeDashArrayProperty = DependencyProperty.Register(
            nameof(StrokeDashArray), typeof(DoubleCollection), typeof(Connection), new PropertyMetadata(null));

        public static readonly DependencyProperty TargetAnchorAngleProperty = DependencyProperty.Register(
            nameof(TargetAnchorAngle), typeof(double), typeof(Connection), new PropertyMetadata(0d));

        public static readonly DependencyProperty TargetAnchorPositionProperty = DependencyProperty.Register(
            nameof(TargetAnchorPosition), typeof(Point), typeof(Connection), new PropertyMetadata(default(Point)));

        public static readonly DependencyProperty TargetArrowSymbolProperty = DependencyProperty.Register(
                                                                    nameof(TargetArrowSymbol), typeof(ArrowSymbol), typeof(Connection), new PropertyMetadata(ArrowSymbol.Arrow));

        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
                                                                    nameof(Target), typeof(Connector), typeof(Connection), new PropertyMetadata(null, OnConnectorPropertyChanged));

        private Adorner _connectionAdorner;

        private WidgetPanel _parentPanel;

        static Connection()
        {
            LabelPositionProperty = LabelPositionPropertyKey.DependencyProperty;
        }

        public Connection(Connector source, Connector target)
        {
            Source = source;
            Target = target;
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public Point LabelPosition
        {
            get => (Point)GetValue(LabelPositionProperty);
            private set => SetValue(LabelPositionPropertyKey, value);
        }

        public PathGeometry PathGeometry
        {
            get => (PathGeometry)GetValue(PathGeometryProperty);
            set => SetValue(PathGeometryProperty, value);
        }

        public Connector Source
        {
            get => (Connector)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public double SourceAnchorAngle
        {
            get => (double)GetValue(SourceAnchorAngleProperty);
            set => SetValue(SourceAnchorAngleProperty, value);
        }

        public Point SourceAnchorPosition
        {
            get => (Point)GetValue(SourceAnchorPositionProperty);
            set => SetValue(SourceAnchorPositionProperty, value);
        }

        public ArrowSymbol SourceArrowSymbol
        {
            get => (ArrowSymbol)GetValue(SourceArrowSymbolProperty);
            set => SetValue(SourceArrowSymbolProperty, value);
        }

        public DoubleCollection StrokeDashArray
        {
            get => (DoubleCollection)GetValue(StrokeDashArrayProperty);
            set => SetValue(StrokeDashArrayProperty, value);
        }

        public Connector Target
        {
            get => (Connector)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        public double TargetAnchorAngle
        {
            get => (double)GetValue(TargetAnchorAngleProperty);
            set => SetValue(TargetAnchorAngleProperty, value);
        }

        public Point TargetAnchorPosition
        {
            get => (Point)GetValue(TargetAnchorPositionProperty);
            set => SetValue(TargetAnchorPositionProperty, value);
        }

        public ArrowSymbol TargetArrowSymbol
        {
            get => (ArrowSymbol)GetValue(TargetArrowSymbolProperty);
            set => SetValue(TargetArrowSymbolProperty, value);
        }

        private WidgetPanel ParentPanel => _parentPanel ?? (_parentPanel = this.GetParentOfType<WidgetPanel>());

        internal void HideAdorner()
        {
            if (_connectionAdorner != null)
            {
                _connectionAdorner.Visibility = Visibility.Collapsed;
            }
        }

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
                oldConnector.Connection = null;
            }

            if (e.NewValue is Connector newConnector)
            {
                positionPropertyDescriptor.AddValueChanged(newConnector, connection.OnConnectorPositionChanged);
                newConnector.Connection = connection;
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
            else
            {
                Debug.WriteLine("oops");
            }
        }
    }
}