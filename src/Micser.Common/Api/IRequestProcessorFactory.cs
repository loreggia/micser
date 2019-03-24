namespace Micser.Common.Api
{
    public interface IRequestProcessorFactory
    {
        IRequestProcessor Create(string name);
    }
}