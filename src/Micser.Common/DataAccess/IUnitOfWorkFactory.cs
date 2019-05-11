namespace Micser.Common.DataAccess
{
    /// <summary>
    /// Provides creation of transactional unit of work objects.
    /// </summary>
    public interface IUnitOfWorkFactory
    {
        /// <summary>
        /// Creates a unit of work.
        /// </summary>
        IUnitOfWork Create();
    }
}