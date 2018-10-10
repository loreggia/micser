using CommonServiceLocator;
using Micser.Engine.Api;
using Micser.Engine.Audio;
using Micser.Engine.DataAccess;
using Micser.Infrastructure;
using Micser.Infrastructure.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using Unity;

namespace Micser.Engine
{
    public partial class MicserService : ServiceBase
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            "Micser");

        private static readonly string DefaultConnectionString = Path.Combine(AppDataFolder, "Database.db");

        private readonly AudioEngine _engine;

        private readonly ICollection<IEngineModule> _plugins;

        private readonly Server _server;

        static MicserService()
        {
            Directory.CreateDirectory(AppDataFolder);
        }

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

            RegisterTypes(container);

            LoadPlugins(container);

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
                    container.RegisterSingletons<IEngineModule>(moduleTypes);
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

        private void RegisterTypes(IUnityContainer container)
        {
            container.RegisterInstance(new ConnectionString(DefaultConnectionString));
            container.RegisterType<Database>();
        }
    }
}