using Micser.Common.Api;
using Prism.Events;

namespace Micser.App.Infrastructure.Api
{
    /// <summary>
    /// An event that contains data sent from the engine to the UI.
    /// </summary>
    public class ApiEvent : PubSubEvent<ApiEvent.ApiData>
    {
        /// <summary>
        /// The data object passed in an <see cref="ApiEvent"/>.
        /// </summary>
        public class ApiData
        {
            /// <inheritdoc />
            public ApiData(string action, object content)
            {
                Action = action;
                Content = content;
            }

            /// <summary>
            /// Gets the action identifying the content.
            /// </summary>
            public string Action { get; }

            /// <summary>
            /// Gets the content that was sent from the engine.
            /// </summary>
            public object Content { get; }

            /// <summary>
            /// Gets or sets the response to be sent back to the engine.
            /// </summary>
            public ApiResponse Response { get; set; }
        }
    }
}