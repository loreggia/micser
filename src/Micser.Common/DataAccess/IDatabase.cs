namespace Micser.Common.DataAccess
{
    public interface IDatabase
    {
        DataContext GetContext();
    }
}