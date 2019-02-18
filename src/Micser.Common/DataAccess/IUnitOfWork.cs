using System;

namespace Micser.Common.DataAccess
{
    public interface IUnitOfWork : IDisposable
    {
        int Complete();

        T GetRepository<T>() where T : class, IRepository;
    }
}