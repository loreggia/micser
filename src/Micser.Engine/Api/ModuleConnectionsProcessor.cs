using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Audio;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Services;
using System.Linq;

namespace Micser.Engine.Api
{
    [RequestProcessorName(Globals.ApiResources.ModuleConnections)]
    public class ModuleConnectionsProcessor : RequestProcessor
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IModuleConnectionService _moduleConnectionService;

        public ModuleConnectionsProcessor(IAudioEngine audioEngine, IModuleConnectionService moduleConnectionService)
        {
            _audioEngine = audioEngine;
            _moduleConnectionService = moduleConnectionService;

            AddAction("getall", _ => GetAll());
            AddAction("delete", id => DeleteConnection(id));
            AddAction("insert", dto => InsertConnection(dto));
            AddAction("update", dto => UpdateConnection(dto));
        }

        private object DeleteConnection(long id)
        {
            _audioEngine.RemoveConnection(id);

            var connection = _moduleConnectionService.Delete(id);

            if (connection == null)
            {
                return false;
            }

            return connection;
        }

        private object GetAll()
        {
            return _moduleConnectionService.GetAll().ToArray();
        }

        private object InsertConnection(ModuleConnectionDto connectionDto)
        {
            if (connectionDto.SourceId <= 0 || connectionDto.TargetId <= 0)
            {
                return false;
            }

            if (!_moduleConnectionService.Insert(connectionDto))
            {
                return false;
            }

            _audioEngine.AddConnection(connectionDto.Id);

            return connectionDto;
        }

        private dynamic UpdateConnection(ModuleConnectionDto connectionDto)
        {
            var existing = _moduleConnectionService.GetById(connectionDto.Id);

            if (existing == null)
            {
                return false;
            }

            _audioEngine.RemoveConnection(connectionDto.Id);

            if (!_moduleConnectionService.Update(connectionDto))
            {
                return false;
            }

            _audioEngine.AddConnection(connectionDto.Id);

            return connectionDto;
        }
    }
}