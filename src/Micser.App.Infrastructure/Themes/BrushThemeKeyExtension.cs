namespace Micser.App.Infrastructure.Themes
{
#pragma warning disable 1591

    public enum BrushThemeKey
    {
        DefaultBackground,
        DefaultForeground,
        DefaultForegroundDisabled,
        DefaultBorder,
        DefaultIcon,
        DefaultIconPrimary,
        DefaultIconHover,
        ConnectionEnd,
        ConnectionLine,
        ConnectionThumb,
        WidgetBackground,
        WidgetForeground,
        WidgetBorder,
        WidgetBorderSelected,
        WidgetHeaderBackground,
        WidgetPanelBackground,
        WidgetPanelGridLine,
        WidgetToolboxBackground,
        StatusBarBackground,
        MenuItemBackgroundHover,
        MenuItemBackgroundActive,
        MenuItemPopupBorder,
        MenuItemArrow,
        MenuItemArrowHover,
        ToolBarButtonBackground,
        ToolBarButtonBackgroundHover,
        ToolBarButtonBackgroundActive,
        SliderThumbBackground,
        SliderLeftBackground,
        ButtonBackground,
        ButtonBackgroundHover,
        ButtonBackgroundPressed,
        ButtonBorder
    }

#pragma warning restore 1591

    /// <summary>
    /// Strongly typed resource keys for dynamic brush resources.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class BrushThemeKeyExtension : ThemeKeyExtension<BrushThemeKey>
    {
        /// <summary>
        /// Creates an instance of the <see cref="BrushThemeKeyExtension"/> class with the specified key.
        /// </summary>
        public BrushThemeKeyExtension(BrushThemeKey key)
            : base(key)
        {
        }
    }
}