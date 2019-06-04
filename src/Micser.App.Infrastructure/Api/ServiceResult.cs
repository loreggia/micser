using Micser.Common.Api;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// Contains the result of an API service call.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public class ServiceResult<TData>
    {
        /// <inheritdoc />
        public ServiceResult(JsonResponse message)
        {
            IsSuccess = message.IsSuccess;
            Message = message.Message;
            Data = message.Content is TData data ? data : default;
            IsConnected = message.IsConnected;
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