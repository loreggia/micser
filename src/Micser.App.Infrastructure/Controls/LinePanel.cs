using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Micser.App.Infrastructure.Controls
{
    /// <summary>
    /// A panel containing a line created from a list of points. Allows smoothing using bezier segments.
    /// </summary>
    /// <remarks>
    /// https://www.codeproject.com/Articles/769055/Interpolate-D-points-usign-Bezier-curves-in-WPF
    /// </remarks>
    [TemplatePart(Name = PartNameCanvas, Type = typeof(Canvas))]
    [TemplatePart(Name = PartNamePath, Type = typeof(Path))]
    public class LinePanel : Control
    {
        public const string PartNameCanvas = "PART_Canvas";
        public const string PartNamePath = "PART_Path";

        public static readonly DependencyProperty IsClosedCurveProperty = DependencyProperty.Register(
            nameof(IsClosedCurve), typeof(bool), typeof(LinePanel), new PropertyMetadata(false, OnIsClosedCurveChanged));

        public static readonly DependencyProperty PathColorProperty = DependencyProperty.Register(
            nameof(PathColor), typeof(Brush), typeof(LinePanel), new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty PathThicknessProperty = DependencyProperty.Register(
            nameof(PathThickness), typeof(double), typeof(LinePanel), new PropertyMetadata(1d));

        public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
            nameof(Points), typeof(IEnumerable<Point>), typeof(LinePanel), new PropertyMetadata(null, OnPointsPropertyChanged));

        public static readonly DependencyProperty SmoothFactorProperty = DependencyProperty.Register(
            nameof(SmoothFactor), typeof(double), typeof(LinePanel), new PropertyMetadata(0.6d, OnSmoothFactorPropertyChanged));

        protected Canvas _canvas;

        protected Path _path;

        public bool IsClosedCurve
        {
            get => (bool)GetValue(IsClosedCurveProperty);
            set => SetValue(IsClosedCurveProperty, value);
        }

        public Brush PathColor
        {
            get => (Brush)GetValue(PathColorProperty);
            set => SetValue(PathColorProperty, value);
        }

        public double PathThickness
        {
            get => (double)GetValue(PathThicknessProperty);
            set => SetValue(PathThicknessProperty, value);
        }

        public IEnumerable<Point> Points
        {
            get => (IEnumerable<Point>)GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        public double SmoothFactor
        {
            get => (double)GetValue(SmoothFactorProperty);
            set => SetValue(SmoothFactorProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _canvas = (Canvas)GetTemplateChild("PART_Canvas");
            _path = (Path)GetTemplateChild("PART_Path");

            UpdatePathData();
        }

        private static List<BezierSegment> InterpolatePointWithBezierCurves(IList<Point> points, double smoothFactor, bool isClosedCurve)
        {
            if (points.Count < 3)
            {
                return null;
            }

            var result = new List<BezierSegment>();

            //if is close curve then add the first point at the end
            if (isClosedCurve)
            {
                points.Add(points.First());
            }

            for (var i = 0; i < points.Count - 1; i++)   //iterate for points but the last one
            {
                // Assume we need to calculate the control
                // points between (x1,y1) and (x2,y2).
                // Then x0,y0 - the previous vertex,
                //      x3,y3 - the next one.
                var x1 = points[i].X;
                var y1 = points[i].Y;

                var x2 = points[i + 1].X;
                var y2 = points[i + 1].Y;

                double x0;
                double y0;

                if (i == 0) //if is first point
                {
                    if (isClosedCurve)
                    {
                        var previousPoint = points[points.Count - 2];    //last Point, but one (due inserted the first at the end)
                        x0 = previousPoint.X;
                        y0 = previousPoint.Y;
                    }
                    else    //Get some previouse point
                    {
                        var previousPoint = points[i];  //if is the first point the previous one will be it self
                        x0 = previousPoint.X;
                        y0 = previousPoint.Y;
                    }
                }
                else
                {
                    x0 = points[i - 1].X;   //Previous Point
                    y0 = points[i - 1].Y;
                }

                double x3, y3;

                if (i == points.Count - 2)    //if is the last point
                {
                    if (isClosedCurve)
                    {
                        var nextPoint = points[1];  //second Point(due inserted the first at the end)
                        x3 = nextPoint.X;
                        y3 = nextPoint.Y;
                    }
                    else    //Get some next point
                    {
                        var nextPoint = points[i + 1];  //if is the last point the next point will be the last one
                        x3 = nextPoint.X;
                        y3 = nextPoint.Y;
                    }
                }
                else
                {
                    x3 = points[i + 2].X;   //Next Point
                    y3 = points[i + 2].Y;
                }

                var xc1 = (x0 + x1) / 2.0;
                var yc1 = (y0 + y1) / 2.0;
                var xc2 = (x1 + x2) / 2.0;
                var yc2 = (y1 + y2) / 2.0;
                var xc3 = (x2 + x3) / 2.0;
                var yc3 = (y2 + y3) / 2.0;

                var len1 = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
                var len2 = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                var len3 = Math.Sqrt((x3 - x2) * (x3 - x2) + (y3 - y2) * (y3 - y2));

                var k1 = len1 / (len1 + len2);
                var k2 = len2 / (len2 + len3);

                var xm1 = xc1 + (xc2 - xc1) * k1;
                var ym1 = yc1 + (yc2 - yc1) * k1;

                var xm2 = xc2 + (xc3 - xc2) * k2;
                var ym2 = yc2 + (yc3 - yc2) * k2;

                // Resulting control points. Here smooth_value is mentioned
                // above coefficient K whose value should be in range [0...1].
                var ctrl1_x = xm1 + (xc2 - xm1) * smoothFactor + x1 - xm1;
                var ctrl1_y = ym1 + (yc2 - ym1) * smoothFactor + y1 - ym1;

                var ctrl2_x = xm2 + (xc2 - xm2) * smoothFactor + x2 - xm2;
                var ctrl2_y = ym2 + (yc2 - ym2) * smoothFactor + y2 - ym2;

                result.Add(new BezierSegment
                {
                    Point1 = i == 0 && !isClosedCurve ? new Point(x1, y1) : new Point(ctrl1_x, ctrl1_y),
                    Point2 = i == points.Count - 2 && !isClosedCurve ? new Point(x2, y2) : new Point(ctrl2_x, ctrl2_y),
                    Point3 = new Point(x2, y2)
                });
            }

            return result;
        }

        private static void OnIsClosedCurveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LinePanel)d).UpdatePathData();
        }

        private static void OnPointsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LinePanel control))
            {
                return;
            }

            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += control.OnPointCollectionChanged;
            }

            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnPointCollectionChanged;
            }

            if (e.NewValue != null)
            {
                control.UpdatePathData();
            }
        }

        private static void OnSmoothFactorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LinePanel)d).UpdatePathData();
        }

        private void OnPointCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdatePathData();
        }

        private void UpdatePathData()
        {
            if (Points == null || _path == null)
            {
                return;
            }

            var points = Points.ToList();

            if (points.Count <= 1)
            {
                return;
            }

            var pathFigure = new PathFigure { StartPoint = points[0] };

            var pathSegments = new PathSegmentCollection();

            var bezierSegments = InterpolatePointWithBezierCurves(points, SmoothFactor, IsClosedCurve);

            if (bezierSegments == null || bezierSegments.Count < 1)
            {
                foreach (var point in points.GetRange(1, points.Count - 1))
                {
                    var myLineSegment = new LineSegment { Point = point };
                    pathSegments.Add(myLineSegment);
                }
            }
            else
            {
                foreach (var bezierSegment in bezierSegments)
                {
                    pathSegments.Add(bezierSegment);
                }
            }

            pathFigure.Segments = pathSegments;

            var myPathFigureCollection = new PathFigureCollection { pathFigure };

            _path.Data = new PathGeometry { Figures = myPathFigureCollection };
        }
    }
}