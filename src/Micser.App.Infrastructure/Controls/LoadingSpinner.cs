using System;
using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Controls
{
    /// <summary>
    /// A loading spinner consisting of a number of rotating circles.
    /// </summary>
    [TemplatePart(Name = PartCanvas, Type = typeof(Canvas))]
    public class LoadingSpinner : Control
    {
        /// <summary>
        /// The name of the canvas control.
        /// </summary>
        public const string PartCanvas = "PART_Canvas";

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="CircleCount"/> property.
        /// </summary>
        public static readonly DependencyProperty CircleCountProperty = DependencyProperty.Register(
            nameof(CircleCount), typeof(int), typeof(LoadingSpinner), new PropertyMetadata(9));

        /// <summary>
        /// <see cref="DependencyProperty"/> for the <see cref="CircleTemplate"/> property.
        /// </summary>
        public static readonly DependencyProperty CircleTemplateProperty = DependencyProperty.Register(
            nameof(CircleTemplate), typeof(DataTemplate), typeof(LoadingSpinner), new PropertyMetadata(default(DataTemplate)));

        protected Canvas Canvas;

        static LoadingSpinner()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LoadingSpinner), new FrameworkPropertyMetadata(typeof(LoadingSpinner)));
        }

        /// <summary>
        /// Gets or sets the number of circles to generate for the spinner.
        /// Wraps the <see cref="CircleCountProperty"/> dependency property.
        /// </summary>
        public int CircleCount
        {
            get => (int)GetValue(CircleCountProperty);
            set => SetValue(CircleCountProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> to use for displaying the spinning circles.
        /// Wraps the <see cref="CircleTemplateProperty"/> dependency property.
        /// </summary>
        public DataTemplate CircleTemplate
        {
            get => (DataTemplate)GetValue(CircleTemplateProperty);
            set => SetValue(CircleTemplateProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (!(GetTemplateChild(PartCanvas) is Canvas canvas))
            {
                throw new InvalidOperationException(PartCanvas);
            }
            Canvas = canvas;

            if (CircleTemplate == null)
            {
                throw new InvalidOperationException(nameof(CircleTemplate));
            }

            var offset = Math.PI;
            var step = Math.PI * 2 / CircleCount;

            for (var i = 0; i < CircleCount; i++)
            {
                if (!(CircleTemplate.LoadContent() is UIElement circle))
                {
                    throw new InvalidOperationException(nameof(CircleTemplate));
                }

                circle.SetValue(Canvas.LeftProperty, 50 + Math.Sin(offset + i * step) * 50);
                circle.SetValue(Canvas.TopProperty, 50 + Math.Cos(offset + i * step) * 50);
                circle.Opacity = 1.0 - 1.0 / CircleCount * i;

                Canvas.Children.Add(circle);
            }
        }
    }
}