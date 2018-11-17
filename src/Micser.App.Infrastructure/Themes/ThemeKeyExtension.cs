using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace Micser.App.Infrastructure.Themes
{
    public class ThemeKeyExtension<T> : ResourceKey
    {
        private readonly T _key;

        public ThemeKeyExtension(T key)
        {
            _key = key;
        }

        public override Assembly Assembly => GetType().Assembly;

        public static bool operator !=(ThemeKeyExtension<T> extension1, ThemeKeyExtension<T> extension2)
        {
            return !(extension1 == extension2);
        }

        public static bool operator ==(ThemeKeyExtension<T> extension1, ThemeKeyExtension<T> extension2)
        {
            return EqualityComparer<ThemeKeyExtension<T>>.Default.Equals(extension1, extension2);
        }

        public override bool Equals(object obj)
        {
            var extension = obj as ThemeKeyExtension<T>;
            return extension != null &&
                   EqualityComparer<T>.Default.Equals(_key, extension._key) &&
                   EqualityComparer<Assembly>.Default.Equals(Assembly, extension.Assembly);
        }

        public override int GetHashCode()
        {
            var hashCode = -401752094;
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(_key);
            hashCode = hashCode * -1521134295 + EqualityComparer<Assembly>.Default.GetHashCode(Assembly);
            return hashCode;
        }
    }
}