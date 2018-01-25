namespace Micser.Infrastructure.Themes
{
    public enum GenericThemeKey
    {
        BackgroundBrush,
        TitleStyle
    }

    public class GenericThemeKeyExtension : ThemeKeyExtension<GenericThemeKey>
    {
        public GenericThemeKeyExtension(GenericThemeKey key)
            : base(key)
        {
        }
    }
}