namespace Micser.App.Infrastructure.Events
{
    public interface IEventAggregator
    {
        T GetEvent<T>();
    }
}