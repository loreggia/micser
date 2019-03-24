using Micser.Common.Api;

namespace Micser.App.Infrastructure.Api
{
    public class ServiceResult<TData>
    {
        public ServiceResult(JsonResponse message)
        {
            IsSuccess = message.IsSuccess;
            Message = message.Message;
            Data = message.Content is TData data ? data : default(TData);
        }

        public TData Data { get; }
        public bool IsSuccess { get; }
        public string Message { get; set; }

        public override string ToString()
        {
            return $"Success: {IsSuccess}, Data: {Data}";
        }
    }
}