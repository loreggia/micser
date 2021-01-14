using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Micser.Common.Controllers;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Common.Services;
using Micser.Models;
using Micser.UI.Infrastructure;

namespace Micser.Controllers
{
    public class ModulesController : ApiController
    {
        private readonly IEnumerable<ModuleDescription> _moduleDescriptions;
        private readonly IModuleService _moduleService;

        public ModulesController(IModuleService moduleService, IEnumerable<ModuleDescription> moduleDescriptions)
        {
            _moduleService = moduleService;
            _moduleDescriptions = moduleDescriptions;
        }

        [HttpPost("")]
        public async Task<ModuleDto> CreateAsync([FromBody] ModuleDto dto)
        {
            var module = GetModel(dto);
            await _moduleService.InsertAsync(module);
            return GetDto(module);
        }

        [HttpDelete("{id?}")]
        public async Task DeleteAsync(long? id = null)
        {
            if (id == null)
            {
                await _moduleService.TruncateAsync();
            }
            else
            {
                await _moduleService.DeleteAsync(id.Value);
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

        [HttpGet("Descriptions")]
        public IEnumerable<ModuleDescription> GetDescriptions()
        {
            return _moduleDescriptions;
        }

        [HttpPut("")]
        public async Task<ModuleDto> UpdateAsync([FromBody] ModuleDto dto)
        {
            if (dto.Id <= 0) throw new BadRequestApiException($"{nameof(ModuleDto.Id)} must be set.");

            var module = GetModel(dto);
            await _moduleService.UpdateAsync(module);
            return GetDto(module);
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

        private Module GetModel(ModuleDto dto)
        {
            if (string.IsNullOrEmpty(dto.Type)) throw new BadRequestApiException($"{nameof(ModuleDto.Type)} must be set.");

            var module = new Module(dto.Id, dto.Type);
            module.State.AddRange(dto.State);
            return module;
        }
    }
}