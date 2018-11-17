namespace Micser.Infrastructure.DataAccess
{
    public interface IDatabase
    {
        DataStore GetContext();
    }
}