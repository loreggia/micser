﻿using Micser.Common.Modules;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.Services
{
    /// <summary>
    /// Provides access to saved engine modules.
    /// </summary>
    public interface IModuleService
    {
        ModuleDto Delete(long id);

        IEnumerable<ModuleDto> GetAll();

        ModuleDto GetById(long id);

        bool Insert(ModuleDto moduleDto);

        bool Truncate();

        bool Update(ModuleDto moduleDto);
    }
}