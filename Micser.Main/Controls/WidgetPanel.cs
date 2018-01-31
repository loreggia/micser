using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Micser.Infrastructure.Extensions;

namespace Micser.Main.Controls
{
    public class WidgetPanel : Canvas
    {
        public const string PartNameCanvas = "PART_Canvas";

        public static readonly DependencyProperty AllowDraggingProperty = DependencyProperty.Register(
            nameof(AllowDragging), typeof(bool), typeof(WidgetPanel), new PropertyMetadata(true));

        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(
            nameof(IsDragging), typeof(bool), typeof(WidgetPanel), new PropertyMetadata(false));

        public static readonly DependencyProperty RasterSizeProperty = DependencyProperty.Register(
            nameof(RasterSize), typeof(double), typeof(WidgetPanel), new PropertyMetadata(25d));

        private UIElement _draggedElement;

        private Point _originalCursorLocation;

        private double _originalLeft;

        private double _originalTop;

        static WidgetPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WidgetPanel), new FrameworkPropertyMetadata(typeof(WidgetPanel)));
        }

        public bool AllowDragging
        {
            get => (bool)GetValue(AllowDraggingProperty);
            set => SetValue(AllowDraggingProperty, value);
        }

        /// <summary>
        /// Returns the UIElement currently being dragged, or null.
        /// </summary>
        /// <remarks>
        /// Note to inheritors: This property exposes a protected
        /// setter which should be used to modify the drag element.
        /// </remarks>
        public UIElement DraggingElement
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
                child.EnsureCanvasTopLeft();

                var left = GetLeft(child);
                var top = GetTop(child);

                desiredSize = new Size(
                    Math.Max(desiredSize.Width, left + child.DesiredSize.Width),
                    Math.Max(desiredSize.Height, top + child.DesiredSize.Height));
            }
            return desiredSize;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            _originalCursorLocation = e.GetPosition(this);

            DraggingElement = FindCanvasChild(e.Source as DependencyObject);

            if (DraggingElement == null)
            {
                return;
            }

            DraggingElement.EnsureCanvasTopLeft();

            _originalLeft = GetLeft(DraggingElement);
            _originalTop = GetTop(DraggingElement);

            e.Handled = true;

            IsDragging = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (DraggingElement == null || !IsDragging)
            {
                return;
            }

            var cursorLocation = e.GetPosition(this);

            var newLeft = _originalLeft + (cursorLocation.X - _originalCursorLocation.X);
            var newTop = _originalTop + (cursorLocation.Y - _originalCursorLocation.Y);

            if (newLeft < 0)
            {
                newLeft = 0;
            }

            if (newTop < 0)
            {
                newTop = 0;
            }

            SetLeft(DraggingElement, newLeft);
            SetTop(DraggingElement, newTop);

            // calling this updates the size of the canvas and lets the surrounding scroll viewer show the scroll bars.
            InvalidateMeasure();
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            // setting DraggedElement to null calls OnPreviewMouseMove, so we need to cache the element for snapping afterwards
            var element = DraggingElement;
            DraggingElement = null;
            SnapToGrid(element);
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualAdded is Widget widget)
            {
            }
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

            var left = GetLeft(element);
            var top = GetTop(element);

            var xSnap = left % RasterSize;
            var ySnap = top % RasterSize;

            if (xSnap <= RasterSize / 2d)
            {
                xSnap *= -1;
            }
            else
            {
                xSnap = RasterSize - xSnap;
            }

            if (ySnap <= RasterSize / 2d)
            {
                ySnap *= -1;
            }
            else
            {
                ySnap = RasterSize - ySnap;
            }

            left += xSnap;
            top += ySnap;

            SetLeft(element, left);
            SetTop(element, top);
        }
    }
}