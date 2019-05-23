using Micser.Common;
using Micser.Common.Api;
using Micser.Common.DataAccess;
using Micser.Common.Extensions;
using Micser.Engine.Api;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.DataAccess;
using Micser.Engine.Infrastructure.DataAccess.Repositories;
using Micser.Engine.Infrastructure.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using Unity;
using Unity.Injection;
using Unity.Resolution;

namespace Micser.Engine
{
    public partial class MicserService : ServiceBase
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly ICollection<IEngineModule> _plugins;
        private IAudioEngine _engine;
        private IApiServer _server;

        static MicserService()
        {
            Directory.CreateDirectory(Globals.AppDataFolder);
        }

        public MicserService()
        {
            InitializeComponent();

            _plugins = new List<IEngineModule>();
        }

        public void ManualStart()
        {
            Logger.Info("Starting service");

            var container = new UnityContainer();

            container.RegisterType<ILogger>(new InjectionFactory(c => LogManager.GetCurrentClassLogger()));

            container.RegisterType<DbContext, EngineDbContext>();
            container.RegisterInstance<IRepositoryFactory>(new RepositoryFactory((t, c) => (IRepository)container.Resolve(t, new ParameterOverride("context", c))));
            container.RegisterInstance<IUnitOfWorkFactory>(new UnitOfWorkFactory(() => container.Resolve<IUnitOfWork>()));
            container.RegisterType<IUnitOfWork, UnitOfWork>();

            container.RegisterType<IModuleRepository, ModuleRepository>();
            container.RegisterType<IModuleConnectionRepository, ModuleConnectionRepository>();

            container.RegisterType<IModuleService, ModuleService>();
            container.RegisterType<IModuleConnectionService, ModuleConnectionService>();

            container.RegisterRequestProcessor<EngineProcessor>();
            container.RegisterRequestProcessor<StatusProcessor>();
            container.RegisterRequestProcessor<ModulesProcessor>();
            container.RegisterRequestProcessor<ModuleConnectionsProcessor>();

            container.RegisterSingleton<IAudioEngine, AudioEngine>();
            container.RegisterSingleton<IApiServer, ApiServer>();
            container.RegisterInstance<IApiConfiguration>(new ApiConfiguration { Port = Globals.ApiPort });

            container.RegisterSingleton<IRequestProcessorFactory, RequestProcessorFactory>();

            _engine = container.Resolve<IAudioEngine>();
            _server = container.Resolve<IApiServer>();

            LoadPlugins(container);

            _server.Start();
            _engine.Start();

            Logger.Info("Service started");
        }

        public void ManualStop()
        {
            Logger.Info("Stopping service");

            _server.Stop();
            _engine.Stop();

            _plugins.Clear();

            Logger.Info("Service stopped");
        }

        protected override void OnStart(string[] args)
        {
            ManualStart();
        }

        protected override void OnStop()
        {
            ManualStop();
        }

        private void LoadPlugins(IUnityContainer container)
        {
            Logger.Info("Loading plugins");

            _plugins.Clear();

            _plugins.Add(new InfrastructureModule());

            var executingFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var moduleFiles = executingFile.Directory.GetFiles(Globals.PluginSearchPattern);
            foreach (var moduleFile in moduleFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFile(moduleFile.FullName);
                    var moduleTypes = assembly.GetExportedTypes().Where(t => typeof(IEngineModule).IsAssignableFrom(t));
                    container.RegisterSingletons<IEngineModule>(moduleTypes);
                    var modules = container.ResolveAll<IEngineModule>();
                    foreach (var engineModule in modules)
                    {
                        _plugins.Add(engineModule);
                        Logger.Info($"Loading plugin {engineModule.GetType().AssemblyQualifiedName}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex);
                }
            }

            foreach (var engineModule in _plugins)
            {
                engineModule.RegisterTypes(container);
            }

            Logger.Info("Plugins loaded");
        }
    }
}