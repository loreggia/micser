using Micser.Infrastructure.Extensions;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity;
using Unity.Lifetime;

namespace Micser.Engine.Api
{
    public class Bootstrapper : NancyBootstrapperWithRequestContainerBase<IUnityContainer>
    {
        private readonly IUnityContainer _container;

        public Bootstrapper(IUnityContainer container = null)
        {
            _container = container;
        }

        protected override IUnityContainer CreateRequestContainer(NancyContext context)
        {
            return _container.CreateChildContainer();
        }

        protected override IEnumerable<INancyModule> GetAllModules(IUnityContainer container)
        {
            return ApplicationContainer.ResolveAll<INancyModule>();
        }

        protected override IUnityContainer GetApplicationContainer()
        {
            return _container ?? new UnityContainer();
        }

        protected override IEnumerable<IApplicationStartup> GetApplicationStartupTasks()
        {
            return ApplicationContainer.ResolveAll<IApplicationStartup>();
        }

        protected override IDiagnostics GetDiagnostics()
        {
            return ApplicationContainer.Resolve<IDiagnostics>();
        }

        protected override INancyEngine GetEngineInternal()
        {
            return ApplicationContainer.Resolve<INancyEngine>();
        }

        protected override INancyModule GetModule(IUnityContainer container, Type moduleType)
        {
            return ApplicationContainer.Resolve(moduleType) as INancyModule;
        }

        protected override IEnumerable<IRegistrations> GetRegistrationTasks()
        {
            return ApplicationContainer.ResolveAll<IRegistrations>();
        }

        protected override IEnumerable<IRequestStartup> RegisterAndGetRequestStartupTasks(IUnityContainer container, Type[] requestStartupTypes)
        {
            container.RegisterTypes<IRequestStartup>(requestStartupTypes, new TransientLifetimeManager());
            return container.ResolveAll<IRequestStartup>();
        }

        protected override void RegisterBootstrapperTypes(IUnityContainer applicationContainer)
        {
            applicationContainer.RegisterInstance<INancyModuleCatalog>(this);
        }

        protected override void RegisterCollectionTypes(IUnityContainer container, IEnumerable<CollectionTypeRegistration> collectionTypeRegistrations)
        {
            foreach (var typeRegistration in collectionTypeRegistrations)
            {
                switch (typeRegistration.Lifetime)
                {
                    case Lifetime.Transient:
                        container.RegisterTypes(typeRegistration.RegistrationType, typeRegistration.ImplementationTypes, new TransientLifetimeManager());
                        continue;
                    case Lifetime.Singleton:
                        container.RegisterTypes(typeRegistration.RegistrationType, typeRegistration.ImplementationTypes, new ContainerControlledLifetimeManager());
                        continue;
                    case Lifetime.PerRequest:
                        throw new InvalidOperationException("Unable to directly register a per request lifetime.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected override void RegisterInstances(IUnityContainer container, IEnumerable<InstanceRegistration> instanceRegistrations)
        {
            foreach (var instanceRegistration in instanceRegistrations)
            {
                container.RegisterInstance(instanceRegistration.RegistrationType, instanceRegistration.Implementation);
            }
        }

        protected override void RegisterRequestContainerModules(IUnityContainer container, IEnumerable<ModuleRegistration> moduleRegistrationTypes)
        {
            container.RegisterTypes<INancyModule>(moduleRegistrationTypes.Select(x => x.ModuleType), new ContainerControlledLifetimeManager());
        }

        protected override void RegisterTypes(IUnityContainer container, IEnumerable<TypeRegistration> typeRegistrations)
        {
            foreach (var typeRegistration in typeRegistrations)
            {
                switch (typeRegistration.Lifetime)
                {
                    case Lifetime.Transient:
                        container.RegisterType(typeRegistration.RegistrationType, typeRegistration.ImplementationType);
                        continue;
                    case Lifetime.Singleton:
                        container.RegisterSingleton(typeRegistration.RegistrationType, typeRegistration.ImplementationType);
                        continue;
                    case Lifetime.PerRequest:
                        throw new InvalidOperationException("Unable to directly register a per request lifetime.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected override void RequestStartup(IUnityContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            if (context.Request.Headers.Accept.All(x => x.Item1 != "application/json"))
            {
                context.Request.Headers.Accept = new[] { new Tuple<string, decimal>("application/json", 1m) }.Concat(context.Request.Headers.Accept);
            }
        }
    }
}