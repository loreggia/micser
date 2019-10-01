using Micser.Common.Extensions;

namespace Micser.Common.Api
{
    /// <summary>
    /// Factory that creates an <see cref="IRequestProcessor"/> instance registered with the incoming request's resource name.
    /// </summary>
    public class RequestProcessorFactory : IRequestProcessorFactory
    {
        private readonly IContainerProvider _container;

        /// <summary>
        /// Creates an instance of the <see cref="RequestProcessorFactory"/> class.
        /// </summary>
        /// <param name="container">The container where the request processors are registered.</param>
        public RequestProcessorFactory(IContainerProvider container)
        {
            _container = container;
        }

        /// <summary>
        /// Creates a request processor that was registered using the <see cref="RequestProcessorNameAttribute"/> attribute with the specified name.
        /// </summary>
        /// <param name="name">The name of the request processor.</param>
        /// <returns>An <see cref="IRequestProcessor"/> that was registered with the name <paramref name="name"/>
        /// or an instance of the <see cref="DefaultRequestProcessor"/> class if no processor with this name was registered.</returns>
        public IRequestProcessor Create(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = RequestProcessorNameAttribute.DefaultName;
            }

            return _container.TryResolve<IRequestProcessor>(name) ??
                   _container.TryResolve<IRequestProcessor>() ??
                   new DefaultRequestProcessor();
        }
    }
}