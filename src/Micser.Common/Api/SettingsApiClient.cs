using Micser.Common.Settings;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// Provides access to settings via API.
    /// </summary>
    public class SettingsApiClient
    {
        private const string ResourceName = Globals.ApiResources.Settings;
        private readonly IApiEndPoint _apiEndPoint;

        /// <inheritdoc />
        public SettingsApiClient(IApiEndPoint apiEndPoint)
        {
            _apiEndPoint = apiEndPoint;
        }

        /// <summary>
        /// Returns an engine setting value.
        /// </summary>
        public async Task<ServiceResult<SettingValueDto>> GetSetting(string key)
        {
            var response = await _apiEndPoint.SendMessageAsync(new JsonRequest(ResourceName, "getsetting", key)).ConfigureAwait(false);
            return new ServiceResult<SettingValueDto>(response);
        }

        /// <summary>
        /// Sets an engine setting value.
        /// </summary>
        public async Task<ServiceResult<SettingValueDto>> SetSetting(string key, object value)
        {
            var response = await _apiEndPoint.SendMessageAsync(new JsonRequest(ResourceName, "setsetting", new SettingValueDto { Key = key, Value = value })).ConfigureAwait(false);
            return new ServiceResult<SettingValueDto>(response);
        }
    }
}