namespace Micser.Common.Api
{
    /// <summary>
    /// A message that represents the response from an API call.
    /// </summary>
    public class ApiResponse : ApiMessage
    {
        /// <inheritdoc />
        public ApiResponse()
        {
        }

        /// <inheritdoc />
        public ApiResponse(bool isSuccess, object content = null, string message = null)
            : base(content)
        {
            IsSuccess = isSuccess;
            Content = content;
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