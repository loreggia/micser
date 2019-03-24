namespace Micser.Common.Api
{
    public interface IRequestProcessor
    {
        JsonResponse Process(string action, object content);
    }
}