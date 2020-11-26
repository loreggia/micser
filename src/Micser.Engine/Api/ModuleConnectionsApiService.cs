using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Micser.Common.Api;
using Micser.Common.Api.Extensions;
using Micser.Common.Audio;
using Micser.Engine.Infrastructure.Services;
using Status = Micser.Common.Api.Status;

namespace Micser.Engine.Api
{
    public class ModuleConnectionsApiService : ModuleConnectionsRpcService.ModuleConnectionsRpcServiceBase
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IModuleConnectionService _moduleConnectionService;

        public ModuleConnectionsApiService(IAudioEngine audioEngine, IModuleConnectionService moduleConnectionService)
        {
            _audioEngine = audioEngine;
            _moduleConnectionService = moduleConnectionService;
        }

        public override async Task<ModuleConnection> Create(ModuleConnection request, ServerCallContext context)
        {
            var connectionDto = request.AsDto();

            if (connectionDto.SourceId <= 0 || connectionDto.TargetId <= 0)
            {
                return null;
            }

            if (!_moduleConnectionService.Insert(connectionDto))
            {
                return null;
            }

            _audioEngine.AddConnection(connectionDto.Id);

            return connectionDto.AsApiModel();
        }

        public override async Task<Status> Delete(Identifiable request, ServerCallContext context)
        {
            _audioEngine.RemoveConnection(request.Id);

            var connection = _moduleConnectionService.Delete(request.Id);

            return new Status { Value = connection != null };
        }

        public override async Task<ModuleConnections> GetAll(Empty request, ServerCallContext context)
        {
            var connections = _moduleConnectionService
                .GetAll()
                .Select(x => x.AsApiModel())
                .ToArray();

            return new ModuleConnections
            {
                Items = { connections }
            };
        }

        public override async Task<ModuleConnection> Update(ModuleConnection request, ServerCallContext context)
        {
            var connectionDto = request.AsDto();

            var existing = _moduleConnectionService.GetById(connectionDto.Id);

            if (existing == null)
            {
                return null;
            }

            _audioEngine.RemoveConnection(connectionDto.Id);

            if (!_moduleConnectionService.Update(connectionDto))
            {
                return null;
            }

            _audioEngine.AddConnection(connectionDto.Id);

            return connectionDto.AsApiModel();
        }
    }
}