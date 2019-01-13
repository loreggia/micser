namespace Micser.Common.DataAccess
{
    public interface IDatabase
    {
        DataStore GetContext();
    }
}