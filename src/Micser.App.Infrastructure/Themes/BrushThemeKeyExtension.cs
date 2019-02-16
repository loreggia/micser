namespace Micser.App.Infrastructure.Themes
{
    public enum BrushThemeKey
    {
        DefaultBackground,
        DefaultForeground,
        DefaultForegroundDisabled,
        DefaultBorder,
        DefaultIcon,
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
        ToolBarButtonBackgroundActive
    }

    public class BrushThemeKeyExtension : ThemeKeyExtension<BrushThemeKey>
    {
        public BrushThemeKeyExtension(BrushThemeKey key)
            : base(key)
        {
        }
    }
}