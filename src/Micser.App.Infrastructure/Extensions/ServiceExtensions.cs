using Micser.App.Infrastructure.Settings;
using Micser.Common.Extensions;

namespace Micser.App.Infrastructure.Extensions
{
    /// <summary>
    /// Contains extension methods for services.
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Wrapper for the <see cref="ISettingsService.GetSetting"/> method that converts the value to type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="settingsService">The settings service.</param>
        /// <param name="key">The setting key.</param>
        /// <typeparam name="T">The type of the setting value.</typeparam>
        /// <returns>The setting value or <c>default(T)</c> if the setting was not found or the value conversion failed.</returns>
        public static T GetSetting<T>(this ISettingsService settingsService, string key)
        {
            return settingsService.GetSetting(key).ToType<T>();
        }
    }
}