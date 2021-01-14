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

        public ModuleService(IDbContextFactory<EngineDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc />
        public async Task<Module?> DeleteAsync(long id)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var entity = await dbContext.Modules.FindAsync(id).ConfigureAwait(false);

            if (entity == null)
            {
                return null;
            }

            dbContext.Modules.Remove(entity);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return ToModel(entity);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Module> GetAllAsync()
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            await foreach (var entity in dbContext.Modules)
            {
                yield return ToModel(entity);
            }
        }

        /// <inheritdoc />
        public async Task<Module?> GetByIdAsync(long id)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var entity = await dbContext.Modules.FindAsync(id).ConfigureAwait(false);
            return entity == null ? null : ToModel(entity);
        }

        /// <inheritdoc />
        public async Task InsertAsync(Module module)
        {
            var entity = ToEntity(module);

            await using var dbContext = _dbContextFactory.CreateDbContext();

            await dbContext.Modules.AddAsync(entity).ConfigureAwait(false);

            if (await dbContext.SaveChangesAsync().ConfigureAwait(false) > 0)
            {
                module.Id = entity.Id;
            }
        }

        /// <inheritdoc />
        public async Task TruncateAsync()
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            await foreach (var entity in dbContext.Modules)
            {
                dbContext.Modules.Remove(entity);
            }
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(Module module)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var entity = await dbContext.Modules.FindAsync(module.Id).ConfigureAwait(false);

            if (entity == null)
            {
                throw new NotFoundApiException();
            }

            var state = JsonConvert.DeserializeObject<ModuleState>(entity.StateJson);
            state.IsEnabled = module.State.IsEnabled;
            state.IsMuted = module.State.IsMuted;
            state.UseSystemVolume = module.State.UseSystemVolume;
            state.Volume = module.State.Volume;

            foreach (var (key, value) in module.State)
            {
                state[key] = value;
            }

            entity.StateJson = JsonConvert.SerializeObject(state);

            module.State = state;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private static ModuleEntity ToEntity(Module module)
        {
            return new ModuleEntity
            {
                Type = module.Type,
                StateJson = JsonConvert.SerializeObject(module.State)
            };
        }

        private static Module ToModel(ModuleEntity module)
        {
            return new Module(module.Id, module.Type)
            {
                State = JsonConvert.DeserializeObject<ModuleState>(module.StateJson)
            };
        }
    }
}