using Micser.Common.Modules;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.Services
{
    /// <summary>
    /// Provides access to saved engine module connections.
    /// </summary>
    public interface IModuleConnectionService
    {
        ModuleConnectionDto Delete(long id);

        IEnumerable<ModuleConnectionDto> GetAll();

        ModuleConnectionDto GetById(long id);

        bool Insert(ModuleConnectionDto dto);

        bool Truncate();

        bool Update(ModuleConnectionDto dto);
    }
}