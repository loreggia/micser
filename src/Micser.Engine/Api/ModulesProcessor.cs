using Micser.Common.Api;
using Micser.Common.Modules;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure.Services;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Api
{
    [RequestProcessorName("modules")]
    public class ModulesProcessor : RequestProcessor
    {
        private readonly IAudioEngine _audioEngine;
        private readonly IModuleService _moduleService;

        public ModulesProcessor(IAudioEngine audioEngine, IModuleService moduleService)
        {
            _audioEngine = audioEngine;
            _moduleService = moduleService;

            this["getall"] = _ => GetAll();
            this["insert"] = dto => InsertModule(dto);
            this["update"] = dto => UpdateModule(dto);
            this["delete"] = id => DeleteModule(id);
            this["changestatedata"] = dto => ChangeStateData(dto);
        }

        private dynamic ChangeStateData(ModuleStateChangeDto dto)
        {
            var moduleDto = _moduleService.GetById(dto.ModuleId);

            if (moduleDto == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(dto.Key))
            {
                return false;
            }

            moduleDto.State.Data[dto.Key] = dto.Value;
            //todo _audioEngine.
            return null;
        }

        private dynamic DeleteModule(long id)
        {
            var module = _moduleService.Delete(id);

            if (module == null)
            {
                return false;
            }

            _audioEngine.DeleteModule(id);

            return module;
        }

        private IEnumerable<ModuleDto> GetAll()
        {
            return _moduleService.GetAll().ToArray();
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