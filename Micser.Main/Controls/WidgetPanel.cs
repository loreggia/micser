using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Micser.Main.Controls
{
    public class WidgetPanel : Canvas
    {
        public const string PartNameCanvas = "PART_Canvas";

        public static readonly DependencyProperty AllowDraggingOutsideProperty = DependencyProperty.Register(
            nameof(AllowDraggingOutside), typeof(bool), typeof(WidgetPanel), new PropertyMetadata(true));

        public static readonly DependencyProperty AllowDraggingProperty = DependencyProperty.Register(
            nameof(AllowDragging), typeof(bool), typeof(WidgetPanel), new PropertyMetadata(true));

        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(
            nameof(IsDragging), typeof(bool), typeof(WidgetPanel), new PropertyMetadata(false));

        public static readonly DependencyProperty RasterSizeProperty = DependencyProperty.Register(
            nameof(RasterSize), typeof(double), typeof(WidgetPanel), new PropertyMetadata(25d));

        private UIElement _draggedElement;

        private bool _modifyLeftOffset;

        private bool _modifyTopOffset;

        private Point _originalCursorLocation;

        private double _originalHorizontalOffset;

        private double _originalVerticalOffset;

        static WidgetPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WidgetPanel), new FrameworkPropertyMetadata(typeof(WidgetPanel)));
        }

        public bool AllowDragging
        {
            get => (bool)GetValue(AllowDraggingProperty);
            set => SetValue(AllowDraggingProperty, value);
        }

        public bool AllowDraggingOutside
        {
            get => (bool)GetValue(AllowDraggingOutsideProperty);
            set => SetValue(AllowDraggingOutsideProperty, value);
        }

        /// <summary>
        /// Returns the UIElement currently being dragged, or null.
        /// </summary>
        /// <remarks>
        /// Note to inheritors: This property exposes a protected
        /// setter which should be used to modify the drag element.
        /// </remarks>
        public UIElement DraggedElement
        {
            get
            {
                if (!AllowDragging)
                {
                    return null;
                }

                return _draggedElement;
            }
            protected set
            {
                _draggedElement?.ReleaseMouseCapture();

                if (!AllowDragging)
                {
                    _draggedElement = null;
                }
                else
                {
                    if (value != null)
                    {
                        _draggedElement = value;
                        _draggedElement.CaptureMouse();
                    }
                    else
                    {
                        _draggedElement = null;
                    }
                }

                IsDragging = _draggedElement != null;
            }
        }

        public bool IsDragging
        {
            get => (bool)GetValue(IsDraggingProperty);
            set => SetValue(IsDraggingProperty, value);
        }

        public double RasterSize
        {
            get => (double)GetValue(RasterSizeProperty);
            set => SetValue(RasterSizeProperty, value);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            base.MeasureOverride(constraint);
            var desiredSize = new Size();
            foreach (UIElement child in Children)
            {
                var horizontalOffset = ResolveOffset(GetLeft(child), GetRight(child), out var left);
                var verticalOffset = ResolveOffset(GetTop(child), GetBottom(child), out var top);

                desiredSize = new Size(
                    Math.Max(desiredSize.Width, left ? horizontalOffset + child.DesiredSize.Width : horizontalOffset),
                    Math.Max(desiredSize.Height, top ? verticalOffset + child.DesiredSize.Height : verticalOffset));
            }
            return desiredSize;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            _originalCursorLocation = e.GetPosition(this);

            DraggedElement = FindCanvasChild(e.Source as DependencyObject);

            if (DraggedElement == null)
            {
                return;
            }

            var left = GetLeft(DraggedElement);
            var right = GetRight(DraggedElement);
            var top = GetTop(DraggedElement);
            var bottom = GetBottom(DraggedElement);

            _originalHorizontalOffset = ResolveOffset(left, right, out _modifyLeftOffset);
            _originalVerticalOffset = ResolveOffset(top, bottom, out _modifyTopOffset);

            e.Handled = true;

            IsDragging = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (DraggedElement == null || !IsDragging)
            {
                return;
            }

            var cursorLocation = e.GetPosition(this);

            double newHorizontalOffset, newVerticalOffset;

            #region Calculate Offsets

            // Determine the horizontal offset.
            if (_modifyLeftOffset)
            {
                newHorizontalOffset = _originalHorizontalOffset + (cursorLocation.X - _originalCursorLocation.X);
            }
            else
            {
                newHorizontalOffset = _originalHorizontalOffset - (cursorLocation.X - _originalCursorLocation.X);
            }

            // Determine the vertical offset.
            if (_modifyTopOffset)
            {
                newVerticalOffset = _originalVerticalOffset + (cursorLocation.Y - _originalCursorLocation.Y);
            }
            else
            {
                newVerticalOffset = _originalVerticalOffset - (cursorLocation.Y - _originalCursorLocation.Y);
            }

            #endregion Calculate Offsets

            #region Verify Drag Element Location

            if (!AllowDraggingOutside)
            {
                // Get the bounding rect of the drag element.
                var elemRect = CalculateDragElementRect(newHorizontalOffset, newVerticalOffset);

                // If the element is being dragged out of the viewable area, determine the ideal rect location, so that the element is within the edge(s) of the canvas.
                var leftAlign = elemRect.Left < 0;
                var rightAlign = elemRect.Right > ActualWidth;

                if (leftAlign)
                {
                    newHorizontalOffset = _modifyLeftOffset ? 0 : ActualWidth - elemRect.Width;
                }
                else if (rightAlign)
                {
                    newHorizontalOffset = _modifyLeftOffset ? ActualWidth - elemRect.Width : 0;
                }

                var topAlign = elemRect.Top < 0;
                var bottomAlign = elemRect.Bottom > ActualHeight;

                if (topAlign)
                {
                    newVerticalOffset = _modifyTopOffset ? 0 : ActualHeight - elemRect.Height;
                }
                else if (bottomAlign)
                {
                    newVerticalOffset = _modifyTopOffset ? ActualHeight - elemRect.Height : 0;
                }
            }

            #endregion Verify Drag Element Location

            #region Move Drag Element

            if (_modifyLeftOffset)
            {
                Debug.WriteLine($"SetLeft {newHorizontalOffset}");
                SetLeft(DraggedElement, newHorizontalOffset);
            }
            else
            {
                Debug.WriteLine($"SetRight {newHorizontalOffset}");
                SetRight(DraggedElement, newHorizontalOffset);
            }

            if (_modifyTopOffset)
            {
                Debug.WriteLine($"SetTop {newVerticalOffset}");
                SetTop(DraggedElement, newVerticalOffset);
            }
            else
            {
                Debug.WriteLine($"SetBottom {newVerticalOffset}");
                SetBottom(DraggedElement, newVerticalOffset);
            }

            #endregion Move Drag Element

            // calling this updates the size of the canvas and lets the surrounding scroll viewer show the scroll bars.
            InvalidateMeasure();
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            // setting DraggedElement to null calls OnPreviewMouseMove, so we need to cache the element for snapping afterwards
            var element = DraggedElement;
            DraggedElement = null;
            SnapToGrid(element);
        }

        /// <summary>
        /// Determines one component of a UIElement's location within a Canvas (either the horizontal or vertical offset).
        /// </summary>
        /// <param name="side1">
        /// The value of an offset relative to a default side of the Canvas (i.e. top or left).
        /// </param>
        /// <param name="side2">
        /// The value of the offset relative to the other side of the Canvas (i.e. bottom or right).
        /// </param>
        /// <param name="useSide1">
        /// Will be set to true if the returned value should be used for the offset from the side represented by the 'side1' parameter. Otherwise, it will be set to false.
        /// </param>
        private static double ResolveOffset(double side1, double side2, out bool useSide1)
        {
            // If the Canvas.Left and Canvas.Right attached properties are specified for an element, the 'Left' value is honored.
            // The 'Top' value is honored if both Canvas.Top and Canvas.Bottom are set on the same element. If one of those attached properties is not set on an element, the default value is Double.NaN.
            useSide1 = true;
            double result;
            if (double.IsNaN(side1))
            {
                if (double.IsNaN(side2))
                {
                    // Both sides have no value, so set the
                    // first side to a value of zero.
                    result = 0;
                }
                else
                {
                    result = side2;
                    useSide1 = false;
                }
            }
            else
            {
                result = side1;
            }
            return result;
        }

        /// <summary>
        /// Returns a Rect which describes the bounds of the element being dragged.
        /// </summary>
        private Rect CalculateDragElementRect(double newHorizOffset, double newVertOffset)
        {
            if (DraggedElement == null)
            {
                throw new InvalidOperationException("DraggedElement is null.");
            }

            var elemSize = DraggedElement.RenderSize;

            double x, y;

            if (_modifyLeftOffset)
            {
                x = newHorizOffset;
            }
            else
            {
                x = ActualWidth - newHorizOffset - elemSize.Width;
            }

            if (_modifyTopOffset)
            {
                y = newVertOffset;
            }
            else
            {
                y = ActualHeight - newVertOffset - elemSize.Height;
            }

            var elemLoc = new Point(x, y);

            return new Rect(elemLoc, elemSize);
        }

        private UIElement FindCanvasChild(DependencyObject depObj)
        {
            while (depObj != null)
            {
                // If the current object is a UIElement which is a child of the Canvas, exit the loop and return it.
                if (depObj is UIElement elem && Children.Contains(elem))
                {
                    break;
                }

                // VisualTreeHelper works with objects of type Visual or Visual3D.
                // If the current object is not derived from Visual or Visual3D, then use the LogicalTreeHelper to find the parent element.
                if (depObj is Visual || depObj is Visual3D)
                {
                    depObj = VisualTreeHelper.GetParent(depObj);
                }
                else
                {
                    depObj = LogicalTreeHelper.GetParent(depObj);
                }
            }
            return depObj as UIElement;
        }

        private void SnapToGrid(UIElement element)
        {
            if (element == null)
            {
                return;
            }

            var horizontalOffset = ResolveOffset(GetLeft(element), GetRight(element), out var isLeft);
            var verticalOffset = ResolveOffset(GetTop(element), GetBottom(element), out var isTop);

            var xPos = isLeft ? horizontalOffset : horizontalOffset - element.DesiredSize.Width;
            var yPos = isTop ? verticalOffset : verticalOffset - element.DesiredSize.Height;

            var xSnap = xPos % RasterSize;
            var ySnap = yPos % RasterSize;

            // If it's less than half the grid size, snap left/up
            // (by subtracting the remainder),
            // otherwise move it the remaining distance of the grid size right/down
            // (by adding the remaining distance to the next grid point).
            if (xSnap <= RasterSize / 2.0)
            {
                xSnap *= -1;
            }
            else
            {
                xSnap = RasterSize - xSnap;
            }

            if (ySnap <= RasterSize / 2.0)
            {
                ySnap *= -1;
            }
            else
            {
                ySnap = RasterSize - ySnap;
            }

            xPos += xSnap;
            yPos += ySnap;

            Debug.WriteLine($"SetLeft {xPos}");
            SetLeft(element, xPos);
            Debug.WriteLine($"SetTop {yPos}");
            SetTop(element, yPos);
        }
    }
}