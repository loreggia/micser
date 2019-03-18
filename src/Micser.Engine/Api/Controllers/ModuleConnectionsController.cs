using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.Services;
using Nancy;
using Nancy.ModelBinding;
using System.Collections.Generic;

namespace Micser.Engine.Api.Controllers
{
    public class ModuleConnectionsController : ApiController
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IModuleConnectionService _moduleConnectionService;

        public ModuleConnectionsController(IAudioEngine audioEngine, IModuleConnectionService moduleConnectionService)
            : base("moduleconnections")
        {
            _audioEngine = audioEngine;
            _moduleConnectionService = moduleConnectionService;

            Get["/"] = _ => GetAll();
            Post["/"] = _ => InsertConnection();
            Put["/{id:long}"] = p => UpdateConnection(p.id);
            Delete["/{id:long}"] = p => DeleteConnection(p.id);
        }

        private dynamic DeleteConnection(long id)
        {
            _audioEngine.RemoveConnection(id);

            var connection = _moduleConnectionService.Delete(id);

            if (connection == null)
            {
                return HttpStatusCode.NotFound;
            }

            return connection;
        }

        private IEnumerable<ModuleConnectionDto> GetAll()
        {
            return _moduleConnectionService.GetAll();
        }

        private dynamic InsertConnection()
        {
            var connectionDto = this.Bind<ModuleConnectionDto>();

            if (connectionDto.SourceId <= 0 || connectionDto.TargetId <= 0)
            {
                return HttpStatusCode.UnprocessableEntity;
            }

            if (!_moduleConnectionService.Insert(connectionDto))
            {
                return HttpStatusCode.InternalServerError;
            }

            _audioEngine.AddConnection(connectionDto.Id);

            return connectionDto;
        }

        private dynamic UpdateConnection(long id)
        {
            var connectionDto = _moduleConnectionService.GetById(id);

            if (connectionDto == null)
            {
                return HttpStatusCode.NotFound;
            }

            this.BindTo(connectionDto);
            connectionDto.Id = id;

            _audioEngine.RemoveConnection(connectionDto.Id);

            if (!_moduleConnectionService.Update(connectionDto))
            {
                return HttpStatusCode.InternalServerError;
            }

            _audioEngine.AddConnection(connectionDto.Id);

            return connectionDto;
        }
    }
}