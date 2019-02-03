namespace Micser.Common.DataAccess
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create();
    }
}