using Micser.Common.DataAccess;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Infrastructure.Services
{
    /// <inheritdoc cref="IModuleService"/>
    public class ModuleService : IModuleService
    {
        private readonly IDbContextFactory _dbContextFactory;

        /// <inheritdoc />
        public ModuleService(IDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc />
        public ModuleDto Delete(long id)
        {
            using var dbContext = _dbContextFactory.Create<EngineDbContext>();

            var module = dbContext.Modules.Find(id);
            if (module == null)
            {
                return null;
            }

            dbContext.Modules.Remove(module);
            dbContext.SaveChanges();

            return GetModuleDto(module);
        }

        /// <inheritdoc />
        public IEnumerable<ModuleDto> GetAll()
        {
            using var dbContext = _dbContextFactory.Create<EngineDbContext>();
            return dbContext.Modules.AsEnumerable().Select(GetModuleDto).ToArray();
        }

        /// <inheritdoc />
        public ModuleDto GetById(long id)
        {
            using var dbContext = _dbContextFactory.Create<EngineDbContext>();
            var module = dbContext.Modules.Find(id);
            return GetModuleDto(module);
        }

        /// <inheritdoc />
        public bool Insert(ModuleDto moduleDto)
        {
            var module = GetModule(moduleDto);

            using var dbContext = _dbContextFactory.Create<EngineDbContext>();
            dbContext.Modules.Add(module);

            if (dbContext.SaveChanges() > 0)
            {
                moduleDto.Id = module.Id;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool Truncate()
        {
            using var dbContext = _dbContextFactory.Create<EngineDbContext>();
            var modules = dbContext.Modules.AsEnumerable();
            dbContext.Modules.RemoveRange(modules);
            return dbContext.SaveChanges() >= 0;
        }

        /// <inheritdoc />
        public bool Update(ModuleDto moduleDto)
        {
            using var dbContext = _dbContextFactory.Create<EngineDbContext>();
            var module = dbContext.Modules.Find(moduleDto.Id);

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

            return dbContext.SaveChanges() >= 0;
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