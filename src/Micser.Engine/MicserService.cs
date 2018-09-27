using Micser.Engine.Api;
using Micser.Engine.Audio;
using Micser.Engine.DataAccess;
using Micser.Shared.Models;
using System.ServiceProcess;

namespace Micser.Engine
{
    public partial class MicserService : ServiceBase
    {
        private readonly Server _server;
        private AudioEngine _engine;

        public MicserService()
        {
            InitializeComponent();

            _server = new Server();
            _engine = new AudioEngine();
        }

        public void ManualStart()
        {
#if DEBUG
            using (var db = new Database())
            {
                var moduleDescriptions = db.GetCollection<ModuleDescription>();
                moduleDescriptions.EnsureIndex(d => d.Id, true);
                var input = new ModuleDescription { Id = 1, State = new DeviceInputModule.DeviceInputState { DeviceId = "{0.0.1.00000000}.{232aa400-5c3f-4f66-b5ab-afe5e2bfb594}" }, Type = "Micser.Engine.Audio.DeviceInputModule, Micser.Engine" };
                moduleDescriptions.Upsert(input);
                var output = new ModuleDescription { Id = 2, State = new DeviceOutputModule.DeviceOutputState { DeviceId = "{0.0.0.00000000}.{04097f83-4fdf-4dae-bfa4-0891f20d1352}" }, Type = "Micser.Engine.Audio.DeviceOutputModule, Micser.Engine" };
                moduleDescriptions.Upsert(output);

                var moduleConnections = db.GetCollection<ModuleConnectionDescription>();
                var connection = new ModuleConnectionDescription { Id = 1, SourceId = input.Id, TargetId = output.Id };
                moduleConnections.Upsert(connection);
            }
#endif

            _server.Start();
            _engine.Start();
        }

        public void ManualStop()
        {
            _server.Stop();
            _engine.Stop();
        }

        protected override void OnStart(string[] args)
        {
            ManualStart();
        }

        protected override void OnStop()
        {
            ManualStop();
        }
    }
}