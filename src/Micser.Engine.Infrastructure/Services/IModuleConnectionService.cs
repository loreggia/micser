using Micser.Common.Modules;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.Services
{
    public interface IModuleConnectionService
    {
        IEnumerable<ModuleConnectionDto> GetAll();

        bool Insert(ModuleConnectionDto dto);

        bool Update(ModuleConnectionDto dto);
    }
}