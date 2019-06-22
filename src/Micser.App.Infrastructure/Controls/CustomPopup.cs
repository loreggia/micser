using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Micser.App.Infrastructure.Controls
{
    /// <summary>
    /// An extended popup control that allows the binding of additional properties unavailable in the default <see cref="Popup"/> control.
    /// </summary>
    public class CustomPopup : Popup
    {
        /// <summary>
        /// The actual horizontal offset taking opposite placement when close to a screen edge into account.
        /// </summary>
        public static readonly DependencyProperty ActualHorizontalOffsetProperty;

        /// <summary>
        /// Property key for the <see cref="ActualHorizontalOffsetProperty"/> dependency property.
        /// </summary>
        public static readonly DependencyPropertyKey ActualHorizontalOffsetPropertyKey;

        /// <summary>
        /// The actual vertical offset taking opposite placement when close to a screen edge into account.
        /// </summary>
        public static readonly DependencyProperty ActualVerticalOffsetProperty;

        /// <summary>
        /// Property key for the <see cref="ActualVerticalOffsetProperty"/> dependency property.
        /// </summary>
        public static readonly DependencyPropertyKey ActualVerticalOffsetPropertyKey;

        /// <summary>
        /// Indicates whether the popup is currently placed on the opposite side due to being close to a screen edge.
        /// </summary>
        public static readonly DependencyProperty IsOppositePlacementProperty;

        /// <summary>
        /// Property key for the <see cref="IsOppositePlacementProperty"/> dependency property.
        /// </summary>
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

        /// <summary>
        /// Gets the actual horizontal offset taking opposite placement when close to a screen edge into account.
        /// </summary>
        public double ActualHorizontalOffset
        {
            get => (double)GetValue(ActualHorizontalOffsetProperty);
            protected set => SetValue(ActualHorizontalOffsetPropertyKey, value);
        }

        /// <summary>
        /// Gets the actual vertical offset taking opposite placement when close to a screen edge into account.
        /// </summary>
        public double ActualVerticalOffset
        {
            get => (double)GetValue(ActualVerticalOffsetProperty);
            protected set => SetValue(ActualVerticalOffsetPropertyKey, value);
        }

        /// <summary>
        /// Gets a value indicating whether the popup is currently placed on the opposite side due to being close to a screen edge.
        /// </summary>
        public bool IsOppositePlacement
        {
            get => (bool)GetValue(IsOppositePlacementProperty);
            protected set => SetValue(IsOppositePlacementPropertyKey, value);
        }

        /// <inheritdoc />
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
                case PlacementMode.Top when offset.Y > 0.1:
                case PlacementMode.Bottom when offset.Y < -0.1:
                case PlacementMode.Left when offset.X > 0.1:
                case PlacementMode.Right when offset.X < -0.1:
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

            if (source?.CompositionTarget == null)
            {
                return new Point();
            }

            return source.CompositionTarget.TransformFromDevice.Transform(popupLocation);
        }
    }
}