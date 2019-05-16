using Micser.App.Infrastructure.Settings;
using Micser.Common.Extensions;

namespace Micser.App.Infrastructure.Extensions
{
    public static class ServiceExtensions
    {
        public static T GetSetting<T>(this ISettingsService settingsService, string key)
        {
            return settingsService.GetSetting(key).ToType<T>();
        }
    }
}