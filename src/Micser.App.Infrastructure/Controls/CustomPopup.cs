using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Controls
{
    public class CustomPopup : Popup
    {
        public static readonly DependencyProperty ActualHorizontalOffsetProperty;
        public static readonly DependencyPropertyKey ActualHorizontalOffsetPropertyKey;
        public static readonly DependencyProperty ActualVerticalOffsetProperty;
        public static readonly DependencyPropertyKey ActualVerticalOffsetPropertyKey;
        public static readonly DependencyProperty IsOppositePlacementProperty;
        public static readonly DependencyPropertyKey IsOppositePlacementPropertyKey;

        static CustomPopup()
        {
            IsOppositePlacementPropertyKey = DependencyProperty.RegisterReadOnly(
                nameof(IsOppositePlacement), typeof(bool), typeof(CustomPopup), new FrameworkPropertyMetadata());
            IsOppositePlacementProperty = IsOppositePlacementPropertyKey.DependencyProperty;

            ActualHorizontalOffsetPropertyKey = DependencyProperty.RegisterReadOnly(
                nameof(ActualHorizontalOffset), typeof(double), typeof(CustomPopup), new FrameworkPropertyMetadata());
            ActualHorizontalOffsetProperty = ActualHorizontalOffsetPropertyKey.DependencyProperty;

            ActualVerticalOffsetPropertyKey = DependencyProperty.RegisterReadOnly(
                nameof(ActualVerticalOffset), typeof(double), typeof(CustomPopup), new FrameworkPropertyMetadata());
            ActualVerticalOffsetProperty = ActualVerticalOffsetPropertyKey.DependencyProperty;
        }

        public double ActualHorizontalOffset
        {
            get => (double)GetValue(ActualHorizontalOffsetProperty);
            protected set => SetValue(ActualHorizontalOffsetPropertyKey, value);
        }

        public double ActualVerticalOffset
        {
            get => (double)GetValue(ActualVerticalOffsetProperty);
            protected set => SetValue(ActualVerticalOffsetPropertyKey, value);
        }

        public bool IsOppositePlacement
        {
            get => (bool)GetValue(IsOppositePlacementProperty);
            protected set => SetValue(IsOppositePlacementPropertyKey, value);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (!(PlacementTarget is FrameworkElement placementTarget) || !(Child is FrameworkElement content))
            {
                return;
            }

            var expectedLocation = GetScreenPosition(placementTarget) + new Vector(HorizontalOffset, VerticalOffset);

            switch (Placement)
            {
                case PlacementMode.Absolute:
                case PlacementMode.Relative:
                case PlacementMode.AbsolutePoint:
                case PlacementMode.RelativePoint:
                case PlacementMode.Mouse:
                case PlacementMode.MousePoint:
                case PlacementMode.Custom:
                    break;

                case PlacementMode.Bottom:
                    expectedLocation.Y += placementTarget.ActualHeight;
                    break;

                case PlacementMode.Center:
                    expectedLocation += new Vector(placementTarget.ActualWidth / 2d, placementTarget.ActualHeight / 2d);
                    break;

                case PlacementMode.Right:
                    expectedLocation.X += placementTarget.ActualWidth;
                    break;

                case PlacementMode.Left:
                    expectedLocation.X -= content.ActualWidth;
                    break;

                case PlacementMode.Top:
                    expectedLocation.Y -= content.ActualHeight;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            var actualLocation = GetScreenPosition(content);
            var offset = actualLocation - expectedLocation;

            var isOppositePlacement = false;

            switch (Placement)
            {
                case PlacementMode.Top when offset.Y > 0:
                case PlacementMode.Bottom when offset.Y < 0:
                case PlacementMode.Left when offset.X > 0:
                case PlacementMode.Right when offset.X < 0:
                    isOppositePlacement = true;
                    break;
            }

            ActualHorizontalOffset = offset.X;
            ActualVerticalOffset = offset.Y;
            IsOppositePlacement = isOppositePlacement;
        }

        private static Point GetScreenPosition(Visual visual)
        {
            var popupLocation = visual.PointToScreen(new Point(0, 0));
            var source = PresentationSource.FromVisual(visual);
            return source.CompositionTarget.TransformFromDevice.Transform(popupLocation);
        }
    }
}