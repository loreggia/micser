namespace Micser.Common.Api
{
    /// <summary>
    /// A message that represents the request of an API call.
    /// </summary>
    public class ApiRequest : ApiMessage
    {
        /// <inheritdoc />
        public ApiRequest()
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="ApiRequest"/> class requesting a specific resource.
        /// </summary>
        public ApiRequest(string resource, string action = null, object content = null)
            : base(content)
        {
            Resource = resource;
            Action = action;
            Content = content;
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
}