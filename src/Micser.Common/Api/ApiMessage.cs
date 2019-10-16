namespace Micser.Common.Api
{
    public abstract class ApiMessage
    {
        public ApiMessage()
        {
        }

        public ApiMessage(object content)
        {
            Content = content;
        }

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public object Content { get; set; }
    }
}