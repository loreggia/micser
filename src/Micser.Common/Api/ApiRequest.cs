using ProtoBuf;

namespace Micser.Common.Api
{
    /// <summary>
    /// A message that represents the request of an API call.
    /// </summary>
    [ProtoContract]
    public sealed class ApiRequest
    {
        /// <inheritdoc />
        public ApiRequest()
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="ApiRequest"/> class requesting a specific resource.
        /// </summary>
        public ApiRequest(string resource, string action = null, object content = null)
        {
            Resource = resource;
            Action = action;
            Content = content;
        }

        /// <summary>
        /// The action to execute.
        /// </summary>
        [ProtoMember(2)]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the raw message content.
        /// </summary>
        [ProtoMember(3, DynamicType = true)]
        public object Content { get; set; }

        /// <summary>
        /// The resource component.
        /// </summary>
        [ProtoMember(1)]
        public string Resource { get; set; }
    }
}