using System.Threading.Tasks;
using Grpc.Core;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Audio;
using Micser.Common.Settings;
using Status = Micser.Common.Api.Status;

namespace Micser.Engine.Api
{
    public class EngineApiService : EngineRpcService.EngineRpcServiceBase
    {
        private readonly IAudioEngine _audioEngine;
        private readonly ISettingsService _settingsService;

        public EngineApiService(IAudioEngine audioEngine, ISettingsService settingsService)
        {
            _audioEngine = audioEngine;
            _settingsService = settingsService;
        }

        public override Task<Status> GetStatus(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new Status
            {
                Value = _audioEngine.IsRunning
            });
        }

        public override async Task<Status> Restart(Empty request, ServerCallContext context)
        {
            _audioEngine.Stop();
            _audioEngine.Start();

            await _settingsService.SetSettingAsync(Globals.SettingKeys.IsEngineRunning, true).ConfigureAwait(false);

            return Success();
        }

        public override async Task<Status> Start(Empty request, ServerCallContext context)
        {
            _audioEngine.Start();

            await _settingsService.SetSettingAsync(Globals.SettingKeys.IsEngineRunning, true).ConfigureAwait(false);

            return Success();
        }

        public override async Task<Status> Stop(Empty request, ServerCallContext context)
        {
            _audioEngine.Stop();

            await _settingsService.SetSettingAsync(Globals.SettingKeys.IsEngineRunning, false).ConfigureAwait(false);

            return Success();
        }

        private static Status Success()
        {
            return new Status { Value = true };
        }
    }
}