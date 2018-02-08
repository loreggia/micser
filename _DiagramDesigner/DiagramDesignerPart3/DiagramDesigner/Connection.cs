﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramDesigner
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
            "IsSelected", typeof(bool), typeof(Connection), new PropertyMetadata(false, OnIsSelectedPropertyChanged));

        public static readonly DependencyProperty LabelPositionProperty = DependencyProperty.Register(
            "LabelPosition", typeof(Point), typeof(Connection), new PropertyMetadata(default(Point)));

        public static readonly DependencyProperty PathGeometryProperty = DependencyProperty.Register(
            "PathGeometry", typeof(PathGeometry), typeof(Connection), new PropertyMetadata(null, OnPathGeometryPropertyChanged));

        public static readonly DependencyProperty SinkAnchorAngleProperty = DependencyProperty.Register(
            "SinkAnchorAngle", typeof(double), typeof(Connection), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty SinkAnchorPositionProperty = DependencyProperty.Register(
            "SinkAnchorPosition", typeof(Point), typeof(Connection), new PropertyMetadata(default(Point)));

        public static readonly DependencyProperty SinkArrowSymbolProperty = DependencyProperty.Register(
            nameof(SinkArrowSymbol), typeof(ArrowSymbol), typeof(Connection), new PropertyMetadata(ArrowSymbol.Arrow));

        public static readonly DependencyProperty SinkProperty = DependencyProperty.Register(
            nameof(Sink), typeof(Connector), typeof(Connection), new PropertyMetadata(null, OnConnectorPropertyChanged));

        public static readonly DependencyProperty SourceAnchorAngleProperty = DependencyProperty.Register(
            "SourceAnchorAngle", typeof(double), typeof(Connection), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty SourceAnchorPositionProperty = DependencyProperty.Register(
            "SourceAnchorPosition", typeof(Point), typeof(Connection), new PropertyMetadata(default(Point)));

        public static readonly DependencyProperty SourceArrowSymbolProperty = DependencyProperty.Register(
            "SourceArrowSymbol", typeof(ArrowSymbol), typeof(Connection), new PropertyMetadata(ArrowSymbol.None));

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(Connector), typeof(Connection), new PropertyMetadata(null, OnConnectorPropertyChanged));

        public static readonly DependencyProperty StrokeDashArrayProperty = DependencyProperty.Register(
            "StrokeDashArray", typeof(DoubleCollection), typeof(Connection), new PropertyMetadata(default(DoubleCollection)));

        private Adorner _connectionAdorner;

        public Connection(Connector source, Connector sink)
        {
            Source = source;
            Sink = sink;
            Unloaded += Connection_Unloaded;
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public Point LabelPosition
        {
            get { return (Point)GetValue(LabelPositionProperty); }
            set { SetValue(LabelPositionProperty, value); }
        }

        public PathGeometry PathGeometry
        {
            get { return (PathGeometry)GetValue(PathGeometryProperty); }
            set { SetValue(PathGeometryProperty, value); }
        }

        public Connector Sink
        {
            get => (Connector)GetValue(SinkProperty);
            set => SetValue(SinkProperty, value);
        }

        public double SinkAnchorAngle
        {
            get { return (double)GetValue(SinkAnchorAngleProperty); }
            set { SetValue(SinkAnchorAngleProperty, value); }
        }

        public Point SinkAnchorPosition
        {
            get { return (Point)GetValue(SinkAnchorPositionProperty); }
            set { SetValue(SinkAnchorPositionProperty, value); }
        }

        public ArrowSymbol SinkArrowSymbol
        {
            get => (ArrowSymbol)GetValue(SinkArrowSymbolProperty);
            set => SetValue(SinkArrowSymbolProperty, value);
        }

        public Connector Source
        {
            get => (Connector)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public double SourceAnchorAngle
        {
            get { return (double)GetValue(SourceAnchorAngleProperty); }
            set { SetValue(SourceAnchorAngleProperty, value); }
        }

        public Point SourceAnchorPosition
        {
            get { return (Point)GetValue(SourceAnchorPositionProperty); }
            set { SetValue(SourceAnchorPositionProperty, value); }
        }

        public ArrowSymbol SourceArrowSymbol
        {
            get { return (ArrowSymbol)GetValue(SourceArrowSymbolProperty); }
            set { SetValue(SourceArrowSymbolProperty, value); }
        }

        public DoubleCollection StrokeDashArray
        {
            get { return (DoubleCollection)GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
        }

        internal void HideAdorner()
        {
            if (_connectionAdorner != null)
                _connectionAdorner.Visibility = Visibility.Collapsed;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            // usual selection business
            if (VisualTreeHelper.GetParent(this) is WidgetPanel panel)
                if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None)
                    if (IsSelected)
                    {
                        IsSelected = false;
                        panel.SelectedItems.Remove(this);
                    }
                    else
                    {
                        IsSelected = true;
                        panel.SelectedItems.Add(this);
                    }
                else if (!IsSelected)
                {
                    foreach (var item in panel.SelectedItems)
                        item.IsSelected = false;

                    panel.SelectedItems.Clear();
                    IsSelected = true;
                    panel.SelectedItems.Add(this);
                }
            e.Handled = false;
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

        private void Connection_Unloaded(object sender, RoutedEventArgs e)
        {
            // do some housekeeping when Connection is unloaded

            // remove event handler
            Source = null;
            Sink = null;

            // remove adorner
            if (_connectionAdorner != null)
            {
                var panel = (WidgetPanel)VisualTreeHelper.GetParent(this);
                if (panel != null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(panel);

                    if (adornerLayer != null)
                    {
                        adornerLayer.Remove(_connectionAdorner);
                        _connectionAdorner = null;
                    }
                }
            }
        }

        private void OnConnectorPositionChanged(object sender, EventArgs e)
        {
            UpdatePathGeometry();
        }

        private void OnConnectorPositionChanged(object sender, PropertyChangedEventArgs e)
        {
            // whenever the 'Position' property of the source or sink Connector
            // changes we must update the connection path geometry
            if (e.PropertyName.Equals("Position"))
            {
                UpdatePathGeometry();
            }
        }

        private void ShowAdorner()
        {
            // the ConnectionAdorner is created once for each Connection
            if (_connectionAdorner == null)
            {
                var panel = VisualTreeHelper.GetParent(this) as WidgetPanel;

                var adornerLayer = AdornerLayer.GetAdornerLayer(panel);
                if (adornerLayer != null)
                {
                    _connectionAdorner = new ConnectionAdorner(panel, this);
                    adornerLayer.Add(_connectionAdorner);
                }
            }
            _connectionAdorner.Visibility = Visibility.Visible;
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
            SinkAnchorAngle = Math.Atan2(pathTangentAtEndPoint.Y, pathTangentAtEndPoint.X) * (180 / Math.PI);

            // add some margin on source and sink side for visual reasons only
            pathStartPoint.Offset(-pathTangentAtStartPoint.X * 5, -pathTangentAtStartPoint.Y * 5);
            pathEndPoint.Offset(pathTangentAtEndPoint.X * 5, pathTangentAtEndPoint.Y * 5);

            SourceAnchorPosition = pathStartPoint;
            SinkAnchorPosition = pathEndPoint;
            LabelPosition = pathMidPoint;
        }

        private void UpdatePathGeometry()
        {
            if (Source != null && Sink != null)
            {
                PathGeometry geometry = new PathGeometry();
                List<Point> linePoints = PathFinder.GetConnectionLine(Source.GetInfo(), Sink.GetInfo(), true);
                if (linePoints.Count > 0)
                {
                    PathFigure figure = new PathFigure();
                    figure.StartPoint = linePoints[0];
                    linePoints.Remove(linePoints[0]);
                    figure.Segments.Add(new PolyLineSegment(linePoints, true));
                    geometry.Figures.Add(figure);

                    this.PathGeometry = geometry;
                }
            }
        }
    }
}