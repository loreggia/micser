﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Micser.Common.Controllers;
using Micser.Common.Modules;
using Micser.Common.Services;
using Micser.Infrastructure;
using Micser.Models;

namespace Micser.Controllers
{
    public class ModuleConnectionsController : ApiController
    {
        private readonly IModuleConnectionService _moduleConnectionService;

        public ModuleConnectionsController(IModuleConnectionService moduleConnectionService)
        {
            _moduleConnectionService = moduleConnectionService;
        }

        [HttpPost("")]
        public async Task<ModuleConnectionDto> CreateAsync([FromBody] ModuleConnectionDto dto)
        {
            var connection = GetModel(dto);
            await _moduleConnectionService.InsertAsync(connection).ConfigureAwait(false);
            return GetDto(connection);
        }

        [HttpGet("")]
        public async IAsyncEnumerable<ModuleConnectionDto> GetAllAsync()
        {
            await foreach (var connection in _moduleConnectionService.GetAllAsync())
            {
                yield return GetDto(connection);
            }
        }

        [HttpPut("{id}")]
        public async Task<ModuleConnectionDto> UpdateAsync([FromRoute] long id, [FromBody] ModuleConnectionDto dto)
        {
            if (id <= 0) throw new BadRequestApiException($"{nameof(id)} must be set.");

            dto.Id = id;
            var connection = GetModel(dto);
            await _moduleConnectionService.UpdateAsync(connection).ConfigureAwait(false);
            return GetDto(connection);
        }

        private static ModuleConnectionDto GetDto(ModuleConnection connection)
        {
            return new ModuleConnectionDto
            {
                Id = connection.Id,
                SourceId = connection.SourceId,
                TargetId = connection.TargetId,
                SourceConnectorName = connection.SourceConnectorName,
                TargetConnectorName = connection.TargetConnectorName
            };
        }

        private static ModuleConnection GetModel(ModuleConnectionDto dto)
        {
            if (dto.SourceId <= 0) throw new BadRequestApiException($"{nameof(ModuleConnectionDto.SourceId)} must be set.");
            if (dto.TargetId <= 0) throw new BadRequestApiException($"{nameof(ModuleConnectionDto.TargetId)} must be set.");
            if (string.IsNullOrEmpty(dto.SourceConnectorName)) throw new BadRequestApiException($"{nameof(ModuleConnectionDto.SourceConnectorName)} must be set.");
            if (string.IsNullOrEmpty(dto.TargetConnectorName)) throw new BadRequestApiException($"{nameof(ModuleConnectionDto.TargetConnectorName)} must be set.");

            return new ModuleConnection(dto.Id, dto.SourceId, dto.SourceConnectorName, dto.TargetId, dto.TargetConnectorName);
        }
    }
}