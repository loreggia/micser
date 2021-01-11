using Micser.Common.Settings;

namespace Micser.Common.Extensions
{
    /// <summary>
    /// Provides extension methods for services.
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Gets the value for the specified setting converted to type <typeparamref name="T"/>.
        /// </summary>
        public static T? GetSetting<T>(this ISettingsService settingsService, string key)
        {
            return settingsService.GetSetting(key).ToType<T>();
        }
    }
}