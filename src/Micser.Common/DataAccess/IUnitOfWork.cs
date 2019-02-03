using Micser.Common.DataAccess.Repositories;
using System;

namespace Micser.Common.DataAccess
{
    public interface IUnitOfWork : IDisposable
    {
        IModuleConnectionRepository ModuleConnections { get; }
        IModuleRepository Modules { get; }

        int Complete();
    }
}