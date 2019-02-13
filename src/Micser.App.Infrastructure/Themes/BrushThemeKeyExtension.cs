namespace Micser.App.Infrastructure.Themes
{
    public enum BrushThemeKey
    {
        DefaultBackground,
        DefaultForeground,
        DefaultForegroundDisabled,
        DefaultBorder,
        ConnectionEnd,
        ConnectionLine,
        ConnectionThumb,
        WidgetBackground,
        WidgetBorder,
        WidgetBorderSelected,
        WidgetHeaderBackground,
        WidgetPanelBackground,
        WidgetPanelGridLine,
        StatusBarBackground,
        MenuItemBackgroundHover,
        MenuItemBackgroundActive,
        MenuItemPopupBorder
    }

    public class BrushThemeKeyExtension : ThemeKeyExtension<BrushThemeKey>
    {
        public BrushThemeKeyExtension(BrushThemeKey key)
            : base(key)
        {
        }
    }
}