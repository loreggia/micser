using Micser.Common.Settings;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// Provides access to settings via API.
    /// </summary>
    public class SettingsApiClient
    {
        private const string ResourceName = Globals.ApiResources.Settings;
        private static readonly bool IsConfigured;
        private readonly IApiClient _apiEndPoint;

        static SettingsApiClient()
        {
            if (!IsConfigured)
            {
                RuntimeTypeModel.Default[typeof(SettingValueDto)].SetSurrogate(typeof(SettingValueSurrogate));
                IsConfigured = true;
            }
        }

        /// <inheritdoc />
        public SettingsApiClient(IApiClient apiEndPoint)
        {
            _apiEndPoint = apiEndPoint;
        }

        /// <summary>
        /// Returns an engine setting value.
        /// </summary>
        public async Task<ServiceResult<SettingValueDto>> GetSetting(string key)
        {
            var response = await _apiEndPoint.SendMessageAsync(new ApiRequest(ResourceName, "getsetting", key)).ConfigureAwait(false);
            return new ServiceResult<SettingValueDto>(response);
        }

        /// <summary>
        /// Sets an engine setting value.
        /// </summary>
        public async Task<ServiceResult<SettingValueDto>> SetSetting(string key, object value)
        {
            var response = await _apiEndPoint.SendMessageAsync(new ApiRequest(ResourceName, "setsetting", new SettingValueDto { Key = key, Value = value })).ConfigureAwait(false);
            return new ServiceResult<SettingValueDto>(response);
        }
    }
}