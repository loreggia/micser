using Micser.Infrastructure;
using Micser.Infrastructure.DataAccess;
using NLog;
using Unity;
using Unity.Injection;

namespace Micser.Engine.Infrastructure
{
    public class InfrastructureEngineModule : IEngineModule
    {
        public void RegisterTypes(IUnityContainer container)
        {
            container.RegisterType<ILogger>(new InjectionFactory(c => LogManager.GetCurrentClassLogger()));
            container.RegisterSingleton<IDatabase>(new InjectionFactory(c => new Database(Globals.AppDbLocation, c.Resolve<ILogger>())));
        }
    }
}