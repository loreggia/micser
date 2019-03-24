using Micser.Common.Api;
using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure.Services;
using System.Linq;

namespace Micser.Engine.Api
{
    [RequestProcessorName("moduleconnections")]
    public class ModuleConnectionsProcessor : RequestProcessor
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IModuleConnectionService _moduleConnectionService;

        public ModuleConnectionsProcessor(IAudioEngine audioEngine, IModuleConnectionService moduleConnectionService)
        {
            _audioEngine = audioEngine;
            _moduleConnectionService = moduleConnectionService;

            this["getall"] = _ => GetAll();
            this["delete"] = id => DeleteConnection(id);
            this["insert"] = dto => InsertConnection(dto);
            this["update"] = dto => UpdateConnection(dto);
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