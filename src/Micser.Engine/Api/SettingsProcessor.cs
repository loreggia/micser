using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Settings;
using System.Threading.Tasks;

namespace Micser.Engine.Api
{
    [RequestProcessorName(Globals.ApiResources.Settings)]
    public class SettingsProcessor : RequestProcessor
    {
        private readonly ISettingsService _settingsService;

        public SettingsProcessor(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            AddAction("getvalue", key => GetValue(key));
            AddAction("setvalue", async dto => await SetValueAsync(dto));
        }

        private object GetValue(string key)
        {
            return new SettingValueDto
            {
                Key = key,
                Value = _settingsService.GetSetting(key)
            };
        }

        private async Task<object> SetValueAsync(SettingValueDto dto)
        {
            await _settingsService.SetSettingAsync(dto.Key, dto.Value);
            return _settingsService.GetSetting(dto.Key);
        }
    }
}