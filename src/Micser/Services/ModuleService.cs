using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Micser.Common.Modules;
using Micser.Common.Services;
using Micser.DataAccess;
using Micser.DataAccess.Entities;
using Micser.UI.Infrastructure;
using Newtonsoft.Json;

namespace Micser.Services
{
    /// <inheritdoc cref="IModuleService"/>
    public class ModuleService : IModuleService
    {
        private readonly IDbContextFactory<EngineDbContext> _dbContextFactory;

        /// <inheritdoc />
        public ModuleService(IDbContextFactory<EngineDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc />
        public async Task<ModuleDto?> DeleteAsync(long id)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var module = await dbContext.Modules.FindAsync(id).ConfigureAwait(false);

            if (module == null)
            {
                return null;
            }

            dbContext.Modules.Remove(module);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return GetModuleDto(module);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ModuleDto> GetAllAsync()
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            await foreach (var module in dbContext.Modules)
            {
                yield return GetModuleDto(module);
            }
        }

        /// <inheritdoc />
        public async Task<ModuleDto?> GetByIdAsync(long id)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var module = await dbContext.Modules.FindAsync(id).ConfigureAwait(false);
            return module == null ? null : GetModuleDto(module);
        }

        /// <inheritdoc />
        public async Task InsertAsync(ModuleDto moduleDto)
        {
            var module = GetModule(moduleDto);

            await using var dbContext = _dbContextFactory.CreateDbContext();

            await dbContext.Modules.AddAsync(module).ConfigureAwait(false);

            if (await dbContext.SaveChangesAsync().ConfigureAwait(false) > 0)
            {
                moduleDto.Id = module.Id;
            }
        }

        /// <inheritdoc />
        public async Task TruncateAsync()
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            await foreach (var module in dbContext.Modules)
            {
                dbContext.Modules.Remove(module);
            }
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(ModuleDto moduleDto)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var module = await dbContext.Modules.FindAsync(moduleDto.Id).ConfigureAwait(false);

            if (module == null)
            {
                throw new NotFoundApiException();
            }

            var state = JsonConvert.DeserializeObject<ModuleState>(module.StateJson);
            state.IsEnabled = moduleDto.State.IsEnabled;
            state.IsMuted = moduleDto.State.IsMuted;
            state.UseSystemVolume = moduleDto.State.UseSystemVolume;
            state.Volume = moduleDto.State.Volume;
            foreach (var data in moduleDto.State)
            {
                state[data.Key] = data.Value;
            }

            module.StateJson = JsonConvert.SerializeObject(state);

            moduleDto.State = state;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private static Module GetModule(ModuleDto moduleDto)
        {
            return new Module
            {
                ModuleType = moduleDto.ModuleType,
                WidgetType = moduleDto.WidgetType,
                StateJson = JsonConvert.SerializeObject(moduleDto.State)
            };
        }

        private static ModuleDto GetModuleDto(Module module)
        {
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