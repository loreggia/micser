using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Settings;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace Micser.Engine.Api
{
    [RequestProcessorName(Globals.ApiResources.Settings)]
    public class SettingsProcessor : RequestProcessor
    {
        private static readonly bool IsConfigured;
        private readonly ISettingsService _settingsService;

        static SettingsProcessor()
        {
            if (!IsConfigured)
            {
                RuntimeTypeModel.Default[typeof(SettingValueDto)].SetSurrogate(typeof(SettingValueSurrogate));
                IsConfigured = true;
            }
        }

        public SettingsProcessor(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            AddAction("getsetting", key => GetValue(key));
            AddAsyncAction("setsetting", dto => SetValueAsync(dto));
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
            await _settingsService.SetSettingAsync(dto.Key, dto.Value).ConfigureAwait(false);
            return GetValue(dto.Key);
        }
    }
}