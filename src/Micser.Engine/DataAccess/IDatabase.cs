namespace Micser.Engine.DataAccess
{
    public interface IDatabase
    {
        DbContext GetContext();
    }
}