using Newtonsoft.Json;

namespace Micser.Common.Api
{
    /// <summary>
    /// A message that can be serialized as JSON.
    /// </summary>
    [JsonConverter(typeof(JsonMessageConverter))]
    public abstract class JsonMessage
    {
        /// <summary>
        /// Creates an instance of the <see cref="JsonMessage"/> class.
        /// </summary>
        protected JsonMessage()
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="JsonMessage"/> class associated with content data.
        /// </summary>
        /// <param name="content">The raw content object to serialize with the message.</param>
        protected JsonMessage(object content)
        {
            Content = content;
        }

        /// <summary>
        /// Gets or sets the raw message content.
        /// </summary>
        public object Content { get; set; }

        /// <summary>
        /// Gets the type name of the current message content.
        /// </summary>
        public string ContentType => Content?.GetType().AssemblyQualifiedName;
    }

    /// <summary>
    /// A message that represents the request of an API call.
    /// </summary>
    public class JsonRequest : JsonMessage
    {
        /// <summary>
        /// Creates an instance of the <see cref="JsonRequest"/> class.
        /// </summary>
        public JsonRequest()
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="JsonRequest"/> class requesting a specific resource.
        /// </summary>
        public JsonRequest(string resource, string action = null, object content = null)
            : base(content)
        {
            Resource = resource;
            Action = action;
        }

        /// <summary>
        /// The action to execute.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// The resource component.
        /// </summary>
        public string Resource { get; set; }
    }

    /// <summary>
    /// A message that represents the response from an API call.
    /// </summary>
    public class JsonResponse : JsonMessage
    {
        /// <summary>
        /// Creates an instance of the <see cref="JsonResponse"/> class.
        /// </summary>
        public JsonResponse()
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="JsonResponse"/> class.
        /// </summary>
        public JsonResponse(bool isSuccess, object content = null, string message = null)
            : base(content)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        /// <summary>
        /// Indicates whether the call was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// An optional message that describes the result of the call.
        /// </summary>
        public string Message { get; set; }
    }
}