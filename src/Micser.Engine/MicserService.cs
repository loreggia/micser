using Micser.Engine.Api;
using System.ServiceProcess;

namespace Micser.Engine
{
    public partial class MicserService : ServiceBase
    {
        private readonly Server _server;

        public MicserService()
        {
            InitializeComponent();

            _server = new Server();
        }

        public void ManualStart()
        {
            _server.Start();
        }

        public void ManualStop()
        {
            _server.Stop();
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