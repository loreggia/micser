using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Controls
{
    [ContentProperty(nameof(Children))]
    [TemplatePart(Name = TemplatePartCanvas, Type = typeof(Canvas))]
    public class ZoomAndPanControl : Control, IAddChild
    {
        public static readonly DependencyProperty GridBackgroundProperty;
        public static readonly DependencyProperty HorizontalOffsetProperty;
        public static readonly DependencyProperty ScaleProperty;
        public static readonly DependencyProperty VerticalOffsetProperty;
        public static readonly DependencyProperty ViewportBottomProperty;
        public static readonly DependencyProperty ViewportHeightProperty;
        public static readonly DependencyProperty ViewportLeftProperty;
        public static readonly DependencyProperty ViewportRightProperty;
        public static readonly DependencyProperty ViewportTopProperty;
        public static readonly DependencyProperty ViewportWidthProperty;

        protected const string TemplatePartCanvas = "PART_Canvas";

        protected static readonly DependencyPropertyKey GridBackgroundPropertyKey;
        protected static readonly DependencyPropertyKey ViewportBottomPropertyKey;
        protected static readonly DependencyPropertyKey ViewportHeightPropertyKey;
        protected static readonly DependencyPropertyKey ViewportLeftPropertyKey;
        protected static readonly DependencyPropertyKey ViewportRightPropertyKey;
        protected static readonly DependencyPropertyKey ViewportTopPropertyKey;
        protected static readonly DependencyPropertyKey ViewportWidthPropertyKey;

        private readonly ScaleTransform _scaleTransform;
        private readonly TransformGroup _transformGroup;
        private readonly TranslateTransform _translateTransform;

        private Canvas? _canvas;

        static ZoomAndPanControl()
        {
            GridBackgroundPropertyKey = DependencyProperty.RegisterReadOnly(nameof(GridBackground), typeof(Brush), typeof(ZoomAndPanControl), new FrameworkPropertyMetadata(null));
            GridBackgroundProperty = GridBackgroundPropertyKey.DependencyProperty;

            HorizontalOffsetProperty = DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(ZoomAndPanControl),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHorizontalOffsetPropertyChanged));
            VerticalOffsetProperty = DependencyProperty.Register(nameof(VerticalOffset), typeof(double), typeof(ZoomAndPanControl),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVerticalOffsetPropertyChanged));
            ScaleProperty = DependencyProperty.Register(nameof(Scale), typeof(double), typeof(ZoomAndPanControl), new FrameworkPropertyMetadata(1d, OnScalePropertyChanged));

            ViewportBottomPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ViewportBottom), typeof(double), typeof(ZoomAndPanControl), new FrameworkPropertyMetadata(0d));
            ViewportBottomProperty = ViewportBottomPropertyKey.DependencyProperty;

            ViewportHeightPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ViewportHeight), typeof(double), typeof(ZoomAndPanControl), new FrameworkPropertyMetadata(0d));
            ViewportHeightProperty = ViewportHeightPropertyKey.DependencyProperty;

            ViewportLeftPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ViewportLeft), typeof(double), typeof(ZoomAndPanControl), new FrameworkPropertyMetadata(0d));
            ViewportLeftProperty = ViewportLeftPropertyKey.DependencyProperty;

            ViewportRightPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ViewportRight), typeof(double), typeof(ZoomAndPanControl), new FrameworkPropertyMetadata(0d));
            ViewportRightProperty = ViewportRightPropertyKey.DependencyProperty;

            ViewportTopPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ViewportTop), typeof(double), typeof(ZoomAndPanControl), new FrameworkPropertyMetadata(0d));
            ViewportTopProperty = ViewportTopPropertyKey.DependencyProperty;

            ViewportWidthPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ViewportWidth), typeof(double), typeof(ZoomAndPanControl), new FrameworkPropertyMetadata(0d));
            ViewportWidthProperty = ViewportWidthPropertyKey.DependencyProperty;
        }

        public ZoomAndPanControl()
        {
            Children = new List<UIElement>();

            _translateTransform = new TranslateTransform();
            _scaleTransform = new ScaleTransform();
            _transformGroup = new TransformGroup
            {
                Children =
                {
                    _translateTransform,
                    _scaleTransform
                }
            };

            GridBackground = new DrawingBrush
            {
                Viewport = new Rect(0d, 0d, 10d, 10d),
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile,
                Drawing = new DrawingGroup
                {
                    Children =
                    {
                        new GeometryDrawing{Brush = Brushes.Black, Geometry = Geometry.Parse("M0,0 L1,0 1,0.1, 0,0.1Z")},
                        new GeometryDrawing{Brush = Brushes.Black, Geometry = Geometry.Parse("M0,0 L0,1 0.1,1, 0.1,0Z")}
                    }
                },
                Transform = _transformGroup
            };

            LayoutUpdated += OnLayoutUpdated;
        }

        public List<UIElement> Children { get; }

        public Brush GridBackground
        {
            get => (Brush)GetValue(GridBackgroundProperty);
            protected set => SetValue(GridBackgroundPropertyKey, value);
        }

        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public double VerticalOffset
        {
            get => (double)GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
        }

        public double ViewportBottom
        {
            get => (double)GetValue(ViewportBottomProperty);
            protected set => SetValue(ViewportBottomPropertyKey, value);
        }

        public double ViewportHeight
        {
            get => (double)GetValue(ViewportHeightProperty);
            protected set => SetValue(ViewportHeightPropertyKey, value);
        }

        public double ViewportLeft
        {
            get => (double)GetValue(ViewportLeftProperty);
            protected set => SetValue(ViewportLeftPropertyKey, value);
        }

        public double ViewportRight
        {
            get => (double)GetValue(ViewportRightProperty);
            protected set => SetValue(ViewportRightPropertyKey, value);
        }

        public double ViewportTop
        {
            get => (double)GetValue(ViewportTopProperty);
            protected set => SetValue(ViewportTopPropertyKey, value);
        }

        public double ViewportWidth
        {
            get => (double)GetValue(ViewportWidthProperty);
            protected set => SetValue(ViewportWidthPropertyKey, value);
        }

        public void AddChild(object value)
        {
            if (_canvas != null)
            {
                ((IAddChild)_canvas).AddChild(value);
            }
            else if (value is UIElement element)
            {
                Children.Add(element);
            }
        }

        public void AddText(string text)
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _canvas = GetTemplateChild(TemplatePartCanvas) as Canvas;
            if (_canvas == null)
            {
                throw new InvalidOperationException($"Template part '{TemplatePartCanvas}' not found.");
            }

            foreach (var child in Children)
            {
                ((IAddChild)_canvas).AddChild(child);
            }

            _canvas.RenderTransform = _transformGroup;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                var scale = Scale + (e.Delta > 0 ? 0.1d : -0.1d);
                if (scale <= 0d)
                {
                    scale = 0.01d;
                }

                Scale = scale;

                var mousePosition = e.GetPosition(this);
                _scaleTransform.CenterX = mousePosition.X;
                _scaleTransform.CenterY = mousePosition.Y;
            }
            else
            {
                var translation = e.Delta * 0.1d;

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    HorizontalOffset += translation;
                }
                else
                {
                    VerticalOffset += translation;
                }
            }
        }

        private static void OnHorizontalOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZoomAndPanControl control && e.NewValue is double value)
            {
                control._translateTransform.X = value;
                control.UpdateViewport();
            }
        }

        private static void OnScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is double value && d is ZoomAndPanControl control)
            {
                control._scaleTransform.ScaleX = value;
                control._scaleTransform.ScaleY = value;
            }
        }

        private static void OnVerticalOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZoomAndPanControl control && e.NewValue is double value)
            {
                control._translateTransform.Y = value;
                control.UpdateViewport();
            }
        }

        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            UpdateViewport();
        }

        private void UpdateViewport()
        {
            if (_canvas == null)
            {
                ViewportLeft = 0d;
                ViewportTop = 0d;
                ViewportRight = ActualWidth;
                ViewportBottom = ActualHeight;
                ViewportWidth = ActualWidth;
                ViewportHeight = ActualHeight;
                return;
            }

            var minX = 0d;
            var maxX = 0d;
            var minY = 0d;
            var maxY = 0d;

            foreach (UIElement? canvasChild in _canvas.Children)
            {
                if (canvasChild == null)
                {
                    continue;
                }

                minX = Math.Min(Canvas.GetLeft(canvasChild), minX);
                maxX = Math.Max(Canvas.GetLeft(canvasChild) + canvasChild.DesiredSize.Width, maxX);
                minY = Math.Min(Canvas.GetTop(canvasChild), minY);
                maxY = Math.Max(Canvas.GetTop(canvasChild) + canvasChild.DesiredSize.Height, maxY);
            }

            ViewportWidth = Math.Max(maxX - minX, ActualWidth);
            ViewportHeight = Math.Max(maxY - minY, ActualHeight);
            ViewportLeft = minX;
            ViewportTop = minY;
            ViewportRight = maxX;
            ViewportBottom = maxY;
        }
    }
}