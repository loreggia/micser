using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Micser.Common.Controllers;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Common.Services;
using Micser.Infrastructure;
using Micser.Models;

namespace Micser.Controllers
{
    public class ModulesController : ApiController
    {
        private readonly IEnumerable<ModuleDefinition> _moduleDefinitions;
        private readonly IModuleService _moduleService;
        private readonly IModuleConnectionService _moduleConnectionService;

        public ModulesController(
            IModuleService moduleService,
            IModuleConnectionService moduleConnectionService,
            IEnumerable<ModuleDefinition> moduleDefinitions)
        {
            _moduleService = moduleService;
            _moduleDefinitions = moduleDefinitions;
            _moduleConnectionService = moduleConnectionService;
        }

        [HttpPost("")]
        public async Task<ModuleDto> CreateAsync([FromBody] ModuleDto dto)
        {
            var module = GetModel(dto);
            await _moduleService.InsertAsync(module).ConfigureAwait(false);
            return GetDto(module);
        }

        [HttpDelete("{id?}")]
        public async Task DeleteAsync(long? id = null)
        {
            if (id == null)
            {
                await _moduleService.TruncateAsync().ConfigureAwait(false);
            }
            else
            {
                await _moduleService.DeleteAsync(id.Value).ConfigureAwait(false);
            }
        }

        [HttpGet("")]
        public async IAsyncEnumerable<ModuleDto> GetAllAsync()
        {
            await foreach (var module in _moduleService.GetAllAsync())
            {
                yield return GetDto(module);
            }
        }

        [HttpGet("Definitions")]
        public IEnumerable<ModuleDefinition> GetDefinitions()
        {
            return _moduleDefinitions;
        }

        [HttpPut("{id}")]
        public async Task<ModuleDto> UpdateAsync([FromRoute] long id, [FromBody] ModuleDto dto)
        {
            if (id <= 0) throw new BadRequestApiException($"{nameof(id)} must be set.");

            dto.Id = id;
            var module = GetModel(dto);
            await _moduleService.UpdateAsync(module).ConfigureAwait(false);
            return GetDto(module);
        }
        public enum ModuleConnectionDirection
        {
            In,
            Out
        }
        [HttpDelete("{id}/connections/{direction?}")]
        public async Task DeleteConnectionsAsync([FromRoute] long id, [FromRoute] ModuleConnectionDirection? direction = null)
        {
            if (id <= 0) throw new BadRequestApiException($"{nameof(id)} must be set.");

            var connections = _moduleConnectionService.GetByModuleIdAsync(id);

            await foreach (var connection in connections)
            {
                switch (direction)
                {
                    case ModuleConnectionDirection.In:
                        if (connection.TargetId == id)
                        {
                            await _moduleConnectionService.DeleteAsync(connection.Id).ConfigureAwait(false);
                        }
                        break;
                    case ModuleConnectionDirection.Out:
                        if (connection.SourceId == id)
                        {
                            await _moduleConnectionService.DeleteAsync(connection.Id).ConfigureAwait(false);
                        }
                        break;
                    default:
                        await _moduleConnectionService.DeleteAsync(connection.Id).ConfigureAwait(false);
                        break;
                }
            }
        }

        private static ModuleDto GetDto(Module module)
        {
            var dto = new ModuleDto
            {
                Id = module.Id,
                Type = module.Type
            };

            dto.State.AddRange(module.State);

            return dto;
        }

        private static Module GetModel(ModuleDto dto)
        {
            if (string.IsNullOrEmpty(dto.Type)) throw new BadRequestApiException($"{nameof(ModuleDto.Type)} must be set.");

            var module = new Module(dto.Id, dto.Type);
            module.State.AddRange(dto.State);
            return module;
        }
    }
}