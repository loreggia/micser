using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Micser.Common.Api;
using Micser.Common.Api.Extensions;
using Micser.Common.Audio;
using Micser.Engine.Infrastructure.Services;
using NLog;
using Status = Micser.Common.Api.Status;

namespace Micser.Engine.Api
{
    public class ModulesApiService : ModulesRpcService.ModulesRpcServiceBase
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IModuleConnectionService _connectionService;
        private readonly ILogger _logger;
        private readonly IModuleService _moduleService;

        public ModulesApiService(IAudioEngine audioEngine, IModuleService moduleService, IModuleConnectionService connectionService, ILogger logger)
        {
            _audioEngine = audioEngine;
            _moduleService = moduleService;
            _connectionService = connectionService;
            _logger = logger;
        }

        public override async Task<Module> Create(Module request, ServerCallContext context)
        {
            var moduleDto = request.AsDto();

            if (string.IsNullOrEmpty(moduleDto?.ModuleType) ||
                string.IsNullOrEmpty(moduleDto.WidgetType))
            {
                return null;
            }

            if (!_moduleService.Insert(moduleDto))
            {
                return null;
            }

            _audioEngine.AddModule(moduleDto.Id);

            return moduleDto.AsApiModel();
        }

        public override async Task<Status> Delete(Identifiable request, ServerCallContext context)
        {
            var module = _moduleService.Delete(request.Id);

            if (module == null)
            {
                return new Status { Value = false };
            }

            _audioEngine.RemoveModule(request.Id);

            return new Status { Value = true };
        }

        public override async Task<Modules> GetAll(Empty request, ServerCallContext context)
        {
            var modules = _moduleService
                .GetAll()
                .Select(x => x.AsApiModel())
                .ToArray();

            return new Modules
            {
                Items = { modules }
            };
        }

        public override async Task<Status> Import(ImportRequest request, ServerCallContext context)
        {
            var state = _audioEngine.IsRunning;

            try
            {
                if (state)
                {
                    _audioEngine.Stop();
                }

                if (!_connectionService.Truncate())
                {
                    _logger.Error("Could not truncate the connections table.");
                    return Status(false);
                }

                if (!_moduleService.Truncate())
                {
                    _logger.Error("Could not truncate the modules table.");
                    return Status(false);
                }

                var moduleIdMap = new Dictionary<long, long>();

                foreach (var module in request.Modules)
                {
                    var oldId = module.Id;
                    var dto = module.AsDto();
                    _moduleService.Insert(dto);
                    moduleIdMap.Add(oldId, dto.Id);
                }

                foreach (var connection in request.Connections)
                {
                    var connectionDto = connection.AsDto();

                    if (moduleIdMap.TryGetValue(connectionDto.SourceId, out var newSourceId) &&
                        moduleIdMap.TryGetValue(connectionDto.TargetId, out var newTargetId))
                    {
                        connectionDto.SourceId = newSourceId;
                        connectionDto.TargetId = newTargetId;
                        _connectionService.Insert(connectionDto);
                    }
                }

                return Status(true);
            }
            finally
            {
                if (state)
                {
                    _audioEngine.Start();
                }
            }
        }

        public override async Task<Module> Update(Module request, ServerCallContext context)
        {
            var moduleDto = request.AsDto();

            var existing = _moduleService.GetById(moduleDto.Id);

            if (existing == null)
            {
                return null;
            }

            existing.State = moduleDto.State;

            if (!_moduleService.Update(existing))
            {
                return null;
            }

            _audioEngine.UpdateModule(existing.Id);

            return existing.AsApiModel();
        }

        private static Status Status(bool value)
        {
            return new Status { Value = value };
        }
    }
}