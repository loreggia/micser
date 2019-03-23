using Newtonsoft.Json;

namespace Micser.Common.Api
{
    [JsonConverter(typeof(JsonMessageConverter))]
    public abstract class JsonMessage
    {
        protected JsonMessage()
        {
        }

        protected JsonMessage(object content)
        {
            Content = content;
        }

        public object Content { get; set; }
        public string ContentType => Content?.GetType().AssemblyQualifiedName;
    }

    public class JsonRequest : JsonMessage
    {
        public JsonRequest()
        {
        }

        public JsonRequest(string action, object content)
            : base(content)
        {
            Action = action;
        }

        public string Action { get; set; }
    }

    public class JsonResponse : JsonMessage
    {
        public JsonResponse()
        {
        }

        public JsonResponse(bool isSuccess, object content, string message)
            : base(content)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}