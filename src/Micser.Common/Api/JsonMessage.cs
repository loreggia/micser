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

        public JsonRequest(string resource, string action = null, object content = null)
            : base(content)
        {
            Resource = resource;
            Action = action;
        }

        public string Action { get; set; }
        public string Resource { get; set; }
    }

    public class JsonResponse : JsonMessage
    {
        public JsonResponse()
        {
        }

        public JsonResponse(bool isSuccess, object content = null, string message = null)
            : base(content)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}