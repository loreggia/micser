using System.Collections.Generic;
using System.Linq;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Models;
using Newtonsoft.Json;

namespace Micser.Engine.Infrastructure.Services
{
    /// <inheritdoc cref="IModuleService"/>
    public class ModuleService : IModuleService
    {
        private readonly EngineDbContext _dbContext;

        /// <inheritdoc />
        public ModuleService(EngineDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public ModuleDto Delete(long id)
        {
            var module = _dbContext.Modules.Find(id);
            if (module == null)
            {
                return null;
            }

            _dbContext.Modules.Remove(module);
            _dbContext.SaveChanges();

            return GetModuleDto(module);
        }

        /// <inheritdoc />
        public IEnumerable<ModuleDto> GetAll()
        {
            return _dbContext.Modules.AsEnumerable().Select(GetModuleDto).ToArray();
        }

        /// <inheritdoc />
        public ModuleDto GetById(long id)
        {
            var module = _dbContext.Modules.Find(id);
            return GetModuleDto(module);
        }

        /// <inheritdoc />
        public bool Insert(ModuleDto moduleDto)
        {
            var module = GetModule(moduleDto);

            _dbContext.Modules.Add(module);

            if (_dbContext.SaveChanges() > 0)
            {
                moduleDto.Id = module.Id;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool Truncate()
        {
            var modules = _dbContext.Modules.AsEnumerable();
            _dbContext.Modules.RemoveRange(modules);
            return _dbContext.SaveChanges() >= 0;
        }

        /// <inheritdoc />
        public bool Update(ModuleDto moduleDto)
        {
            var module = _dbContext.Modules.Find(moduleDto.Id);

            if (module == null)
            {
                return false;
            }

            var state = JsonConvert.DeserializeObject<ModuleState>(module.StateJson);
            state.IsEnabled = moduleDto.State.IsEnabled;
            state.IsMuted = moduleDto.State.IsMuted;
            state.UseSystemVolume = moduleDto.State.UseSystemVolume;
            state.Volume = moduleDto.State.Volume;
            foreach (var data in moduleDto.State.Data)
            {
                state.Data[data.Key] = data.Value;
            }

            module.StateJson = JsonConvert.SerializeObject(state);

            moduleDto.State = state;

            return _dbContext.SaveChanges() >= 0;
        }

        private static Module GetModule(ModuleDto moduleDto)
        {
            if (moduleDto == null)
            {
                return null;
            }

            return new Module
            {
                ModuleType = moduleDto.ModuleType,
                WidgetType = moduleDto.WidgetType,
                StateJson = JsonConvert.SerializeObject(moduleDto.State)
            };
        }

        private static ModuleDto GetModuleDto(Module module)
        {
            if (module == null)
            {
                return null;
            }

            return new ModuleDto
            {
                Id = module.Id,
                ModuleType = module.ModuleType,
                WidgetType = module.WidgetType,
                State = JsonConvert.DeserializeObject<ModuleState>(module.StateJson)
            };
        }
    }
}