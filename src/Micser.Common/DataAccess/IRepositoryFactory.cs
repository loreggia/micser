using System.Data.Entity;

namespace Micser.Common.DataAccess
{
    public interface IRepositoryFactory
    {
        T Create<T>(DbContext context) where T : class;
    }
}