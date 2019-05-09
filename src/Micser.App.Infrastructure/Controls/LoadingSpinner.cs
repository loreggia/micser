using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Controls
{
    /// <summary>
    /// A loading spinner consisting of a number of rotating circles.
    /// </summary>
    [TemplatePart(Name = PartCanvas, Type = typeof(Canvas))]
    [TemplatePart(Name = PartRotationTransform, Type = typeof(RotateTransform))]
    public class LoadingSpinner : Control
    {
        public const string PartCanvas = "PART_Canvas";
        public const string PartRotationTransform = "PART_RotationTransform";

        public static readonly DependencyProperty CircleCountProperty = DependencyProperty.Register(
            nameof(CircleCount), typeof(int), typeof(LoadingSpinner), new PropertyMetadata(9));

        public static readonly DependencyProperty CircleTemplateProperty = DependencyProperty.Register(
                    nameof(CircleTemplate), typeof(DataTemplate), typeof(LoadingSpinner), new PropertyMetadata(default(DataTemplate)));

        private Canvas _canvas;
        private RotateTransform _rotateTransform;

        static LoadingSpinner()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LoadingSpinner), new FrameworkPropertyMetadata(typeof(LoadingSpinner)));
        }

        public int CircleCount
        {
            get => (int)GetValue(CircleCountProperty);
            set => SetValue(CircleCountProperty, value);
        }

        public DataTemplate CircleTemplate
        {
            get => (DataTemplate)GetValue(CircleTemplateProperty);
            set => SetValue(CircleTemplateProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (!(GetTemplateChild(PartCanvas) is Canvas canvas))
            {
                throw new InvalidOperationException(PartCanvas);
            }
            _canvas = canvas;

            if (!(GetTemplateChild(PartRotationTransform) is RotateTransform transform))
            {
                throw new InvalidOperationException(PartRotationTransform);
            }
            _rotateTransform = transform;

            if (CircleTemplate == null)
            {
                throw new InvalidOperationException(nameof(CircleTemplate));
            }

            var offset = Math.PI;
            var step = Math.PI * 2 / CircleCount;

            for (var i = 0; i < CircleCount; i++)
            {
                var circle = CircleTemplate.LoadContent() as UIElement;

                if (circle == null)
                {
                    throw new InvalidOperationException(nameof(CircleTemplate));
                }

                circle.SetValue(Canvas.LeftProperty, 50 + Math.Sin(offset + i * step) * 50);
                circle.SetValue(Canvas.TopProperty, 50 + Math.Cos(offset + i * step) * 50);
                circle.Opacity = 1.0 - 1.0 / CircleCount * i;

                _canvas.Children.Add(circle);
            }
        }
    }
}