using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Localization;
using Micser.Common.Settings;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Micser.App.Settings
{
    public class LanguageSettingHandler : IListSettingHandler
    {
        public IDictionary<object, string> CreateList()
        {
            return AppGlobals.AvailableCultures.ToDictionary<string, object, string>(c => c, c => new CultureInfo(c).NativeName);
        }

        public Task<object> LoadSettingAsync(object value)
        {
            return Task.FromResult(SetCulture(value));
        }

        public Task<object> SaveSettingAsync(object value)
        {
            return Task.FromResult(SetCulture(value));
        }

        private static object SetCulture(object value)
        {
            if (value is string cultureCode)
            {
                try
                {
                    var culture = new CultureInfo(cultureCode);
                    LocalizationManager.UiCulture = culture;
                    return cultureCode;
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }
    }
}