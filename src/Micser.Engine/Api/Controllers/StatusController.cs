using Micser.Common;
using Micser.Engine.Audio;
using Micser.Engine.Infrastructure;
using System.Reflection;

namespace Micser.Engine.Api.Controllers
{
    public class StatusController : ApiController
    {
        private readonly IAudioEngine _audioEngine;

        public StatusController(IAudioEngine audioEngine)
            : base("status")
        {
            _audioEngine = audioEngine;
            Get["/"] = _ => GetStatus();
        }

        private dynamic GetStatus()
        {
            return new ServiceStatus
            {
                IsEngineRunning = _audioEngine.IsRunning,
                EngineVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            };
        }
    }
}