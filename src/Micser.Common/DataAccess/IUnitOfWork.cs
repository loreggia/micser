using System;

namespace Micser.Common.DataAccess
{
    /// <summary>
    /// A unit of cohesive database work.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Completes the workload and returns the number of affected rows.
        /// </summary>
        /// <returns></returns>
        int Complete();

        /// <summary>
        /// Gets an <see cref="IRepository"/> instance of the specified type.
        /// </summary>
        T GetRepository<T>() where T : class, IRepository;
    }
}