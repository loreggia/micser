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

            return connectionDto;
        }

        private dynamic UpdateConnection(long id)
        {
            var connection = _moduleConnectionService.GetById(id);

            if (connection == null)
            {
                return HttpStatusCode.NotFound;
            }

            var connectionDto = this.Bind<ModuleConnectionDto>();

            if (connectionDto.SourceId <= 0 || connectionDto.TargetId <= 0)
            {
                return HttpStatusCode.UnprocessableEntity;
            }

            connection.SourceId = connectionDto.SourceId;
            connection.TargetId = connectionDto.TargetId;

            if (!_moduleConnectionService.Update(connection))
            {
                return HttpStatusCode.InternalServerError;
            }

            return connection;
        }
    }
}