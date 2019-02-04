using Micser.Common.Modules;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.Services
{
    public interface IModuleConnectionService
    {
        ModuleConnectionDto Delete(long id);

        IEnumerable<ModuleConnectionDto> GetAll();

        ModuleConnectionDto GetById(long id);

        bool Insert(ModuleConnectionDto dto);

        bool Update(ModuleConnectionDto dto);
    }
}