using Micser.Engine.Api;
using Micser.Engine.Audio;
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