using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Audio;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Services;
using NLog;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Api
{
    [RequestProcessorName(Globals.ApiResources.Modules)]
    public class ModulesProcessor : RequestProcessor
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IModuleConnectionService _connectionService;
        private readonly ILogger _logger;
        private readonly IModuleService _moduleService;

        public ModulesProcessor(IAudioEngine audioEngine, IModuleService moduleService, IModuleConnectionService connectionService, ILogger logger)
        {
            _audioEngine = audioEngine;
            _moduleService = moduleService;
            _connectionService = connectionService;
            _logger = logger;

            AddAction("getall", _ => GetAll());
            AddAction("insert", dto => InsertModule(dto));
            AddAction("update", dto => UpdateModule(dto));
            AddAction("delete", id => DeleteModule(id));
            AddAction("import", dto => ImportConfiguration(dto));
        }

        private dynamic DeleteModule(long id)
        {
            var module = _moduleService.Delete(id);

            if (module == null)
            {
                return false;
            }

            _audioEngine.RemoveModule(id);

            return module;
        }

        private IEnumerable<ModuleDto> GetAll()
        {
            return _moduleService.GetAll().ToArray();
        }

        private dynamic ImportConfiguration(ModulesExportDto dto)
        {
            var state = _audioEngine.IsRunning;

            try
            {
                if (state)
                {
                    _audioEngine.Stop();
                }

                if (dto?.Modules == null)
                {
                    return false;
                }

                if (!_connectionService.Truncate())
                {
                    _logger.Error("Could not truncate the connections table.");
                    return false;
                }

                if (!_moduleService.Truncate())
                {
                    _logger.Error("Could not truncate the modules table.");
                    return false;
                }

                var moduleIdMap = new Dictionary<long, long>();

                foreach (var moduleDto in dto.Modules)
                {
                    var oldId = moduleDto.Id;
                    _moduleService.Insert(moduleDto);
                    moduleIdMap.Add(oldId, moduleDto.Id);
                }

                foreach (var connectionDto in dto.Connections)
                {
                    if (moduleIdMap.TryGetValue(connectionDto.SourceId, out var newSourceId) &&
                        moduleIdMap.TryGetValue(connectionDto.TargetId, out var newTargetId))
                    {
                        connectionDto.SourceId = newSourceId;
                        connectionDto.TargetId = newTargetId;
                        _connectionService.Insert(connectionDto);
                    }
                }

                return true;
            }
            finally
            {
                if (state)
                {
                    _audioEngine.Start();
                }
            }
        }

        private dynamic InsertModule(ModuleDto moduleDto)
        {
            if (string.IsNullOrEmpty(moduleDto?.ModuleType) ||
                string.IsNullOrEmpty(moduleDto.WidgetType))
            {
                return false;
            }

            if (!_moduleService.Insert(moduleDto))
            {
                return false;
            }

            _audioEngine.AddModule(moduleDto.Id);

            return moduleDto;
        }

        private dynamic UpdateModule(ModuleDto moduleDto)
        {
            var existing = _moduleService.GetById(moduleDto.Id);

            if (existing == null)
            {
                return false;
            }

            existing.State = moduleDto.State;

            if (!_moduleService.Update(existing))
            {
                return false;
            }

            _audioEngine.UpdateModule(existing.Id);

            return existing;
        }
    }
}