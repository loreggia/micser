using Unity;

namespace Micser.Common.Api
{
    public class RequestProcessorFactory : IRequestProcessorFactory
    {
        private readonly IUnityContainer _container;

        public RequestProcessorFactory(IUnityContainer container)
        {
            _container = container;
        }

        public IRequestProcessor Create(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = RequestProcessorNameAttribute.DefaultName;
            }

            return _container.Resolve<IRequestProcessor>(name);
        }
    }
}