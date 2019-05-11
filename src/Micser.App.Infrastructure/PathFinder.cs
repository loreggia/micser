using Micser.App.Infrastructure.Widgets;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Helper class to provide orthogonal connection paths.
    /// </summary>
    /// <remarks>
    /// https://www.codeproject.com/Articles/24681/WPF-Diagram-Designer-Part-4
    /// </remarks>
    public static class PathFinder
    {
#pragma warning disable 1591
        private const double Epsilon = 0.001d;
        private const int Margin = 20;

        public static List<Point> GetConnectionLine(ConnectorInfo source, ConnectorInfo target, bool showLastLine)
        {
            var linePoints = new List<Point>();

            var rectSource = GetRectWithMargin(source, Margin);
            var rectTarget = GetRectWithMargin(target, Margin);

            var startPoint = GetOffsetPoint(source, rectSource);
            var endPoint = GetOffsetPoint(target, rectTarget);

            linePoints.Add(startPoint);
            var currentPoint = startPoint;

            if (!rectTarget.Contains(currentPoint) && !rectSource.Contains(endPoint))
            {
                while (true)
                {
                    #region source node

                    if (IsPointVisible(currentPoint, endPoint, new[] { rectSource, rectTarget }))
                    {
                        linePoints.Add(endPoint);
                        break;
                    }

                    var neighbour = GetNearestVisibleNeighborTarget(currentPoint, endPoint, target, rectSource, rectTarget);

                    if (!double.IsNaN(neighbour.X))
                    {
                        linePoints.Add(neighbour);
                        linePoints.Add(endPoint);
                        break;
                    }

                    if (currentPoint == startPoint)
                    {
                        var n = GetNearestNeighborSource(source, endPoint, rectSource, rectTarget, out var flag);
                        linePoints.Add(n);
                        currentPoint = n;

                        if (!IsRectVisible(currentPoint, rectTarget, new[] { rectSource }))
                        {
                            GetOppositeCorners(source.Orientation, rectSource, out var n1, out var n2);

                            if (flag)
                            {
                                linePoints.Add(n1);
                                currentPoint = n1;
                            }
                            else
                            {
                                linePoints.Add(n2);
                                currentPoint = n2;
                            }

                            if (!IsRectVisible(currentPoint, rectTarget, new[] { rectSource }))
                            {
                                if (flag)
                                {
                                    linePoints.Add(n2);
                                    currentPoint = n2;
                                }
                                else
                                {
                                    linePoints.Add(n1);
                                    currentPoint = n1;
                                }
                            }
                        }
                    }

                    #endregion source node

                    #region target node

                    else // from here on we jump to the target node
                    {
                        GetNeighborCorners(target.Orientation, rectTarget, out var s1, out var s2);
                        GetOppositeCorners(target.Orientation, rectTarget, out var n1, out var n2);

                        var n1Visible = IsPointVisible(currentPoint, n1, new[] { rectSource, rectTarget });
                        var n2Visible = IsPointVisible(currentPoint, n2, new[] { rectSource, rectTarget });

                        if (n1Visible && n2Visible)
                        {
                            if (rectSource.Contains(n1))
                            {
                                linePoints.Add(n2);
                                if (rectSource.Contains(s2))
                                {
                                    linePoints.Add(n1);
                                    linePoints.Add(s1);
                                }
                                else
                                {
                                    linePoints.Add(s2);
                                }

                                linePoints.Add(endPoint);
                                break;
                            }

                            if (rectSource.Contains(n2))
                            {
                                linePoints.Add(n1);
                                if (rectSource.Contains(s1))
                                {
                                    linePoints.Add(n2);
                                    linePoints.Add(s2);
                                }
                                else
                                {
                                    linePoints.Add(s1);
                                }

                                linePoints.Add(endPoint);
                                break;
                            }

                            if (Distance(n1, endPoint) <= Distance(n2, endPoint))
                            {
                                linePoints.Add(n1);
                                if (rectSource.Contains(s1))
                                {
                                    linePoints.Add(n2);
                                    linePoints.Add(s2);
                                }
                                else
                                {
                                    linePoints.Add(s1);
                                }

                                linePoints.Add(endPoint);
                                break;
                            }

                            linePoints.Add(n2);
                            if (rectSource.Contains(s2))
                            {
                                linePoints.Add(n1);
                                linePoints.Add(s1);
                            }
                            else
                            {
                                linePoints.Add(s2);
                            }

                            linePoints.Add(endPoint);
                            break;
                        }

                        if (n1Visible)
                        {
                            linePoints.Add(n1);
                            if (rectSource.Contains(s1))
                            {
                                linePoints.Add(n2);
                                linePoints.Add(s2);
                            }
                            else
                            {
                                linePoints.Add(s1);
                            }

                            linePoints.Add(endPoint);
                            break;
                        }

                        linePoints.Add(n2);
                        if (rectSource.Contains(s2))
                        {
                            linePoints.Add(n1);
                            linePoints.Add(s1);
                        }
                        else
                        {
                            linePoints.Add(s2);
                        }

                        linePoints.Add(endPoint);
                        break;
                    }

                    #endregion target node
                }
            }
            else
            {
                linePoints.Add(endPoint);
            }

            linePoints = OptimizeLinePoints(linePoints, new[] { rectSource, rectTarget }, source.Orientation, target.Orientation);

            CheckPathEnd(source, target, showLastLine, linePoints);
            return linePoints;
        }

        public static List<Point> GetConnectionLine(ConnectorInfo source, Point targetPoint, ConnectorOrientation preferredOrientation)
        {
            var linePoints = new List<Point>();
            var rectSource = GetRectWithMargin(source, 10);
            var startPoint = GetOffsetPoint(source, rectSource);
            var endPoint = targetPoint;

            linePoints.Add(startPoint);
            var currentPoint = startPoint;

            if (!rectSource.Contains(endPoint))
            {
                while (true)
                {
                    if (IsPointVisible(currentPoint, endPoint, new[] { rectSource }))
                    {
                        linePoints.Add(endPoint);
                        break;
                    }

                    var n = GetNearestNeighborSource(source, endPoint, rectSource, out var sideFlag);
                    linePoints.Add(n);
                    currentPoint = n;

                    if (IsPointVisible(currentPoint, endPoint, new[] { rectSource }))
                    {
                        linePoints.Add(endPoint);
                        break;
                    }

                    GetOppositeCorners(source.Orientation, rectSource, out var n1, out var n2);
                    linePoints.Add(sideFlag ? n1 : n2);
                    linePoints.Add(endPoint);
                    break;
                }
            }
            else
            {
                linePoints.Add(endPoint);
            }

            linePoints = OptimizeLinePoints(linePoints, new[] { rectSource }, source.Orientation,
                                            preferredOrientation != ConnectorOrientation.None
                                                ? preferredOrientation
                                                : GetOpositeOrientation(source.Orientation));

            return linePoints;
        }

        private static void CheckPathEnd(ConnectorInfo source, ConnectorInfo target, bool showLastLine, List<Point> linePoints)
        {
            if (showLastLine)
            {
                var startPoint = new Point(0, 0);
                var endPoint = new Point(0, 0);
                double marginPath = 15;
                switch (source.Orientation)
                {
                    case ConnectorOrientation.Left:
                        startPoint = new Point(source.Position.X - marginPath, source.Position.Y);
                        break;

                    case ConnectorOrientation.Top:
                        startPoint = new Point(source.Position.X, source.Position.Y - marginPath);
                        break;

                    case ConnectorOrientation.Right:
                        startPoint = new Point(source.Position.X + marginPath, source.Position.Y);
                        break;

                    case ConnectorOrientation.Bottom:
                        startPoint = new Point(source.Position.X, source.Position.Y + marginPath);
                        break;
                }

                switch (target.Orientation)
                {
                    case ConnectorOrientation.Left:
                        endPoint = new Point(target.Position.X - marginPath, target.Position.Y);
                        break;

                    case ConnectorOrientation.Top:
                        endPoint = new Point(target.Position.X, target.Position.Y - marginPath);
                        break;

                    case ConnectorOrientation.Right:
                        endPoint = new Point(target.Position.X + marginPath, target.Position.Y);
                        break;

                    case ConnectorOrientation.Bottom:
                        endPoint = new Point(target.Position.X, target.Position.Y + marginPath);
                        break;
                }

                linePoints.Insert(0, startPoint);
                linePoints.Add(endPoint);
            }
            else
            {
                linePoints.Insert(0, source.Position);
                linePoints.Add(target.Position);
            }
        }

        private static double Distance(Point p1, Point p2)
        {
            return Point.Subtract(p1, p2).Length;
        }

        private static Point GetNearestNeighborSource(ConnectorInfo source, Point endPoint, Rect rectSource, Rect rectTarget, out bool flag)
        {
            GetNeighborCorners(source.Orientation, rectSource, out var n1, out var n2);

            if (rectTarget.Contains(n1))
            {
                flag = false;
                return n2;
            }

            if (rectTarget.Contains(n2))
            {
                flag = true;
                return n1;
            }

            if (Distance(n1, endPoint) <= Distance(n2, endPoint))
            {
                flag = true;
                return n1;
            }

            flag = false;
            return n2;
        }

        private static Point GetNearestNeighborSource(ConnectorInfo source, Point endPoint, Rect rectSource, out bool flag)
        {
            GetNeighborCorners(source.Orientation, rectSource, out var n1, out var n2);

            if (Distance(n1, endPoint) <= Distance(n2, endPoint))
            {
                flag = true;
                return n1;
            }

            flag = false;
            return n2;
        }

        private static Point GetNearestVisibleNeighborTarget(Point currentPoint, Point endPoint, ConnectorInfo target, Rect rectSource, Rect rectTarget)
        {
            GetNeighborCorners(target.Orientation, rectTarget, out var s1, out var s2);

            var flag1 = IsPointVisible(currentPoint, s1, new[] { rectSource, rectTarget });
            var flag2 = IsPointVisible(currentPoint, s2, new[] { rectSource, rectTarget });

            if (flag1) // s1 visible
            {
                if (flag2) // s1 and s2 visible
                {
                    if (rectTarget.Contains(s1))
                    {
                        return s2;
                    }

                    if (rectTarget.Contains(s2))
                    {
                        return s1;
                    }

                    if (Distance(s1, endPoint) <= Distance(s2, endPoint))
                    {
                        return s1;
                    }

                    return s2;
                }

                return s1;
            }

            if (flag2) // only s2 visible
            {
                return s2;
            }

            return new Point(double.NaN, double.NaN);
        }

        private static void GetNeighborCorners(ConnectorOrientation orientation, Rect rect, out Point n1, out Point n2)
        {
            switch (orientation)
            {
                case ConnectorOrientation.Left:
                    n1 = rect.TopLeft;
                    n2 = rect.BottomLeft;
                    break;

                case ConnectorOrientation.Top:
                    n1 = rect.TopLeft;
                    n2 = rect.TopRight;
                    break;

                case ConnectorOrientation.Right:
                    n1 = rect.TopRight;
                    n2 = rect.BottomRight;
                    break;

                case ConnectorOrientation.Bottom:
                    n1 = rect.BottomLeft;
                    n2 = rect.BottomRight;
                    break;

                default:
                    throw new Exception("No neighbour corners found!");
            }
        }

        private static Point GetOffsetPoint(ConnectorInfo connector, Rect rect)
        {
            var offsetPoint = new Point();

            switch (connector.Orientation)
            {
                case ConnectorOrientation.Left:
                    offsetPoint = new Point(rect.Left, connector.Position.Y);
                    break;

                case ConnectorOrientation.Top:
                    offsetPoint = new Point(connector.Position.X, rect.Top);
                    break;

                case ConnectorOrientation.Right:
                    offsetPoint = new Point(rect.Right, connector.Position.Y);
                    break;

                case ConnectorOrientation.Bottom:
                    offsetPoint = new Point(connector.Position.X, rect.Bottom);
                    break;
            }

            return offsetPoint;
        }

        private static ConnectorOrientation GetOpositeOrientation(ConnectorOrientation connectorOrientation)
        {
            switch (connectorOrientation)
            {
                case ConnectorOrientation.Left:
                    return ConnectorOrientation.Right;

                case ConnectorOrientation.Top:
                    return ConnectorOrientation.Bottom;

                case ConnectorOrientation.Right:
                    return ConnectorOrientation.Left;

                case ConnectorOrientation.Bottom:
                    return ConnectorOrientation.Top;

                default:
                    return ConnectorOrientation.Top;
            }
        }

        private static void GetOppositeCorners(ConnectorOrientation orientation, Rect rect, out Point n1, out Point n2)
        {
            switch (orientation)
            {
                case ConnectorOrientation.Left:
                    n1 = rect.TopRight;
                    n2 = rect.BottomRight;
                    break;

                case ConnectorOrientation.Top:
                    n1 = rect.BottomLeft;
                    n2 = rect.BottomRight;
                    break;

                case ConnectorOrientation.Right:
                    n1 = rect.TopLeft;
                    n2 = rect.BottomLeft;
                    break;

                case ConnectorOrientation.Bottom:
                    n1 = rect.TopLeft;
                    n2 = rect.TopRight;
                    break;

                default:
                    throw new Exception("No opposite corners found!");
            }
        }

        private static ConnectorOrientation GetOrientation(Point p1, Point p2)
        {
            if (Math.Abs(p1.X - p2.X) < Epsilon)
            {
                return p1.Y >= p2.Y ? ConnectorOrientation.Bottom : ConnectorOrientation.Top;
            }

            if (Math.Abs(p1.Y - p2.Y) < Epsilon)
            {
                return p1.X >= p2.X ? ConnectorOrientation.Right : ConnectorOrientation.Left;
            }

            throw new Exception("Failed to retrieve orientation");
        }

        private static Rect GetRectWithMargin(ConnectorInfo connectorThumb, double margin)
        {
            var rect = new Rect(connectorThumb.ParentLeft,
                                connectorThumb.ParentTop,
                                connectorThumb.ParentSize.Width,
                                connectorThumb.ParentSize.Height);

            rect.Inflate(margin, margin);

            return rect;
        }

        private static bool IsPointVisible(Point fromPoint, Point targetPoint, Rect[] rectangles)
        {
            foreach (var rect in rectangles)
            {
                if (RectangleIntersectsLine(rect, fromPoint, targetPoint))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsRectVisible(Point fromPoint, Rect targetRect, Rect[] rectangles)
        {
            if (IsPointVisible(fromPoint, targetRect.TopLeft, rectangles))
            {
                return true;
            }

            if (IsPointVisible(fromPoint, targetRect.TopRight, rectangles))
            {
                return true;
            }

            if (IsPointVisible(fromPoint, targetRect.BottomLeft, rectangles))
            {
                return true;
            }

            if (IsPointVisible(fromPoint, targetRect.BottomRight, rectangles))
            {
                return true;
            }

            return false;
        }

        private static List<Point> OptimizeLinePoints(IList<Point> linePoints, Rect[] rectangles, ConnectorOrientation sourceOrientation,
                                                      ConnectorOrientation targetOrientation)
        {
            var points = new List<Point>();
            var cut = 0;

            for (var i = 0; i < linePoints.Count; i++)
            {
                if (i >= cut)
                {
                    for (var k = linePoints.Count - 1; k > i; k--)
                    {
                        if (IsPointVisible(linePoints[i], linePoints[k], rectangles))
                        {
                            cut = k;
                            break;
                        }
                    }

                    points.Add(linePoints[i]);
                }
            }

            #region Line

            for (var j = 0; j < points.Count - 1; j++)
            {
                if (Math.Abs(points[j].X - points[j + 1].X) > Epsilon && Math.Abs(points[j].Y - points[j + 1].Y) > Epsilon)
                {
                    // orientation from point
                    var orientationFrom = j == 0 ? sourceOrientation : GetOrientation(points[j], points[j - 1]);

                    // orientation to pint
                    var orientationTo = j == points.Count - 2 ? targetOrientation : GetOrientation(points[j + 1], points[j + 2]);

                    if ((orientationFrom == ConnectorOrientation.Left || orientationFrom == ConnectorOrientation.Right) &&
                        (orientationTo == ConnectorOrientation.Left || orientationTo == ConnectorOrientation.Right))
                    {
                        var centerX = Math.Min(points[j].X, points[j + 1].X) + Math.Abs(points[j].X - points[j + 1].X) / 2;
                        points.Insert(j + 1, new Point(centerX, points[j].Y));
                        points.Insert(j + 2, new Point(centerX, points[j + 2].Y));

                        if (points.Count - 1 > j + 3)
                        {
                            points.RemoveAt(j + 3);
                        }

                        return points;
                    }

                    if ((orientationFrom == ConnectorOrientation.Top || orientationFrom == ConnectorOrientation.Bottom) &&
                        (orientationTo == ConnectorOrientation.Top || orientationTo == ConnectorOrientation.Bottom))
                    {
                        var centerY = Math.Min(points[j].Y, points[j + 1].Y) + Math.Abs(points[j].Y - points[j + 1].Y) / 2;
                        points.Insert(j + 1, new Point(points[j].X, centerY));
                        points.Insert(j + 2, new Point(points[j + 2].X, centerY));

                        if (points.Count - 1 > j + 3)
                        {
                            points.RemoveAt(j + 3);
                        }

                        return points;
                    }

                    if ((orientationFrom == ConnectorOrientation.Left || orientationFrom == ConnectorOrientation.Right) &&
                        (orientationTo == ConnectorOrientation.Top || orientationTo == ConnectorOrientation.Bottom))
                    {
                        points.Insert(j + 1, new Point(points[j + 1].X, points[j].Y));
                        return points;
                    }

                    if ((orientationFrom == ConnectorOrientation.Top || orientationFrom == ConnectorOrientation.Bottom) &&
                        (orientationTo == ConnectorOrientation.Left || orientationTo == ConnectorOrientation.Right))
                    {
                        points.Insert(j + 1, new Point(points[j].X, points[j + 1].Y));
                        return points;
                    }
                }
            }

            #endregion Line

            return points;
        }

        private static bool RectangleIntersectsLine(Rect rect, Point startPoint, Point endPoint)
        {
            rect.Inflate(-1, -1);
            return rect.IntersectsWith(new Rect(startPoint, endPoint));
        }

#pragma warning restore 1591
    }
}