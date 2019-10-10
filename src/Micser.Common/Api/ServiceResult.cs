namespace Micser.Common.Api
{
    /// <summary>
    /// Contains the result of an API service call.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public class ServiceResult<TData>
    {
        /// <inheritdoc />
        public ServiceResult(ApiResponse message, bool isConnected = true)
        {
            IsSuccess = message.IsSuccess;
            Message = message.Message;
            Data = message.Content is TData data ? data : default;
            IsConnected = isConnected;//todo
        }

        /// <summary>
        /// Gets the data returned from the service.
        /// </summary>
        public TData Data { get; }

        /// <summary>
        /// Gets a value indicating whether a connection to the server is established.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Gets a value whether the call was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets an optional message that was sent by the service or error information in case the call was not successful.
        /// </summary>
        public string Message { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Success: {IsSuccess}, Data: {Data}, Message: {Message}";
        }
    }
}