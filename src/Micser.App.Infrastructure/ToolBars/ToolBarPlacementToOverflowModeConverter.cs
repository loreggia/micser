﻿using Micser.App.Infrastructure.Converter;
using System;
using System.Globalization;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.ToolBars
{
    /// <summary>
    /// Converts a <see cref="ToolBarItemPlacement"/> value to it's corresponding <see cref="OverflowMode"/>.
    /// </summary>
    public class ToolBarPlacementToOverflowModeConverter : ConverterExtension
    {
        /// <inheritdoc />
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToolBarItemPlacement placement)
            {
                switch (placement)
                {
                    case ToolBarItemPlacement.ToolBar:
                        return OverflowMode.Never;

                    case ToolBarItemPlacement.Overflow:
                        return OverflowMode.Always;

                    case ToolBarItemPlacement.Auto:
                        return OverflowMode.AsNeeded;
                }
            }

            return value;
        }

        /// <inheritdoc />
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OverflowMode mode)
            {
                switch (mode)
                {
                    case OverflowMode.AsNeeded:
                        return ToolBarItemPlacement.Auto;

                    case OverflowMode.Always:
                        return ToolBarItemPlacement.Overflow;

                    case OverflowMode.Never:
                        return ToolBarItemPlacement.ToolBar;
                }
            }

            return value;
        }
    }
}