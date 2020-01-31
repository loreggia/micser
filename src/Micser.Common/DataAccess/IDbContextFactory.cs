using Microsoft.EntityFrameworkCore;

namespace Micser.Common.DataAccess
{
    public interface IDbContextFactory
    {
        DbContext Create();
    }
}