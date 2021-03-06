﻿using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// An <see cref="ItemsControl"/> to display all available widgets.
    /// </summary>
    public class WidgetToolbox : ItemsControl
    {
        /// <summary>
        /// Creates a <see cref="WidgetToolboxItem"/>.
        /// </summary>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new WidgetToolboxItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is WidgetToolboxItem;
        }
    }
}