using System.Data.Entity;

namespace Micser.Common.DataAccess
{
    /// <summary>
    /// Factory that creates repositories.
    /// </summary>
    public interface IRepositoryFactory
    {
        /// <summary>
        /// Creates a
        /// </summary>
        /// <param name="context"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Create<T>(DbContext context) where T : class, IRepository;
    }
}