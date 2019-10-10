using ProtoBuf;

namespace Micser.Common.Api
{
    /// <summary>
    /// A message that represents the response from an API call.
    /// </summary>
    [ProtoContract]
    public sealed class ApiResponse
    {
        /// <inheritdoc />
        public ApiResponse()
        {
        }

        /// <inheritdoc />
        public ApiResponse(bool isSuccess, object content = null, string message = null)
        {
            IsSuccess = isSuccess;
            Content = content;
            Message = message;
        }

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        [ProtoMember(3, DynamicType = true)]
        public object Content { get; set; }

        /// <summary>
        /// Indicates whether the call was successful.
        /// </summary>
        [ProtoMember(1)]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// An optional message that describes the result of the call.
        /// </summary>
        [ProtoMember(2)]
        public string Message { get; set; }
    }
}