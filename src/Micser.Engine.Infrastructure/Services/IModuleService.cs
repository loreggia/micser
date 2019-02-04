using Micser.Common.Modules;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.Services
{
    public interface IModuleService
    {
        ModuleDto Delete(long id);

        IEnumerable<ModuleDto> GetAll();

        ModuleDto GetById(long id);

        bool Insert(ModuleDto moduleDto);

        bool Update(ModuleDto moduleDto);
    }
}