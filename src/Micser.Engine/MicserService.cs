using CommonServiceLocator;
using Micser.Engine.Api;
using Micser.Engine.Audio;
using Micser.Engine.DataAccess;
using Micser.Infrastructure;
using Micser.Infrastructure.Extensions;
using Micser.Infrastructure.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using Unity;
using Unity.Lifetime;
using Module = Micser.Infrastructure.Models.Module;

namespace Micser.Engine
{
    public partial class MicserService : ServiceBase
    {
        private readonly AudioEngine _engine;
        private readonly ICollection<IEngineModule> _plugins;
        private readonly Server _server;

        public MicserService()
        {
            InitializeComponent();

            _plugins = new List<IEngineModule>();

            _engine = new AudioEngine();
            _server = new Server();
        }

        public void ManualStart()
        {
            var container = new UnityContainer();

            LoadPlugins(container);

#if DEBUG
            using (var db = new Database())
            {
                var moduleDescriptions = db.GetCollection<Module>();
                moduleDescriptions.EnsureIndex(d => d.Id, true);
                var input = new Module { Id = 1, State = new DeviceInputModule.DeviceInputState { DeviceId = "{0.0.1.00000000}.{232aa400-5c3f-4f66-b5ab-afe5e2bfb594}" }, Type = "Micser.Engine.Audio.DeviceInputModule, Micser.Engine" };
                moduleDescriptions.Upsert(input);
                var output = new Module { Id = 2, State = new DeviceOutputModule.DeviceOutputState { DeviceId = "{0.0.0.00000000}.{04097f83-4fdf-4dae-bfa4-0891f20d1352}" }, Type = "Micser.Engine.Audio.DeviceOutputModule, Micser.Engine" };
                moduleDescriptions.Upsert(output);

                var moduleConnections = db.GetCollection<ModuleConnection>();
                var connection = new ModuleConnection(input.Id, output.Id) { Id = 1 };
                moduleConnections.Upsert(connection);
            }
#endif

            _server.Start(container);
            _engine.Start(container);
        }

        public void ManualStop()
        {
            _server.Stop();
            _engine.Stop();

            _plugins.Clear();
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
            _plugins.Clear();

            var executingFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var moduleFiles = executingFile.Directory.GetFiles(Globals.PluginSearchPattern);
            foreach (var moduleFile in moduleFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFile(moduleFile.FullName);
                    var moduleTypes = assembly.GetExportedTypes().Where(t => typeof(IEngineModule).IsAssignableFrom(t));
                    container.RegisterTypes<IEngineModule>(moduleTypes, new ContainerControlledLifetimeManager());
                    var modules = container.ResolveAll<IEngineModule>();
                    foreach (var engineModule in modules)
                    {
                        engineModule.RegisterTypes(container);
                        _plugins.Add(engineModule);
                    }
                }
                catch (Exception ex)
                {
                    var logger = ServiceLocator.Current.GetInstance<ILogger>();
                    logger.Debug(ex);
                    Debug.WriteLine(ex);
                }
            }
        }
    }
}