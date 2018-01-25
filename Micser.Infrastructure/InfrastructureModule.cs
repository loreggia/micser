using System.Collections.Generic;
using Unity;

namespace Micser.Infrastructure
{
    public class InfrastructureModule : IModuleWithResources
    {
        private readonly IUnityContainer _container;

        public InfrastructureModule(IUnityContainer container)
        {
            _container = container;
        }

        public IEnumerable<string> ResourcePaths
        {
            get { yield return "Micser.Infrastructure;component/Themes/Generic.xaml"; }
        }

        public void Initialize()
        {
            _container.RegisterInstance<IModuleWithResources>("InfrastructureModule", this);
        }
    }
}