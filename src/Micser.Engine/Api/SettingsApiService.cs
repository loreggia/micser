using System.Threading.Tasks;
using Grpc.Core;
using Micser.Common.Api;
using Micser.Common.Extensions;
using Micser.Common.Settings;

namespace Micser.Engine.Api
{
    public class SettingsApiService : SettingsRpcService.SettingsRpcServiceBase
    {
        private readonly ISettingsService _settingsService;

        public SettingsApiService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public override async Task<Setting> GetSetting(SettingKey request, ServerCallContext context)
        {
            return new Setting
            {
                Key = request.Key,
                Value = _settingsService.GetSetting<string>(request.Key)
            };
        }

        public override async Task<Setting> SetSetting(Setting request, ServerCallContext context)
        {
            await _settingsService.SetSettingAsync(request.Key, request.Value).ConfigureAwait(false);
            return new Setting
            {
                Key = request.Key,
                Value = _settingsService.GetSetting<string>(request.Key)
            };
        }
    }
}