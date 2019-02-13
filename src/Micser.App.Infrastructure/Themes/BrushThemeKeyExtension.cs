namespace Micser.App.Infrastructure.Themes
{
    public enum BrushThemeKey
    {
        DefaultBackground,
        DefaultForeground,
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
        StatusBarBackground
    }

    public class BrushThemeKeyExtension : ThemeKeyExtension<BrushThemeKey>
    {
        public BrushThemeKeyExtension(BrushThemeKey key)
            : base(key)
        {
        }
    }
}