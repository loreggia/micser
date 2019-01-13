using CommonServiceLocator;
using Micser.Common;
using Micser.Common.DataAccess;
using Micser.Common.Extensions;
using Micser.Engine.Api;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using Unity;
using Unity.Injection;

namespace Micser.Engine
{
    public partial class MicserService : ServiceBase
    {
        private readonly ICollection<IEngineModule> _plugins;
        private IAudioEngine _engine;
        private IServer _server;

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
            var container = new UnityContainer();

            container.RegisterType<ILogger>(new InjectionFactory(c => LogManager.GetCurrentClassLogger()));
            container.RegisterSingleton<IDatabase>(new InjectionFactory(c => new Database(Globals.EngineDbLocation, c.Resolve<ILogger>())));
            container.RegisterSingleton<IAudioEngine, AudioEngine>();
            container.RegisterSingleton<IServer, Server>();

            _engine = container.Resolve<IAudioEngine>();
            _server = container.Resolve<IServer>();

            LoadPlugins(container);

            _server.Start();
            _engine.Start();
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
                    }
                }
                catch (Exception ex)
                {
                    var logger = ServiceLocator.Current.GetInstance<ILogger>();
                    logger.Debug(ex);
                    Debug.WriteLine(ex);
                }
            }

            foreach (var engineModule in _plugins)
            {
                engineModule.RegisterTypes(container);
            }
        }
    }
}