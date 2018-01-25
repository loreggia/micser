namespace Micser.Infrastructure.Themes
{
    public enum GenericThemeKey
    {
        Title
    }

    public class GenericThemeKeyExtension : ThemeKeyExtension<GenericThemeKey>
    {
        public GenericThemeKeyExtension(GenericThemeKey key)
            : base(key)
        {
        }
    }
}