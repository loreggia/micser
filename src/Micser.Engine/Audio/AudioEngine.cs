using Micser.Engine.DataAccess;
using Micser.Shared.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Engine.Audio
{
    public sealed class AudioEngine : IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public AudioEngine()
        {
            Modules = new List<IAudioModule>();
        }

        public ICollection<IAudioModule> Modules { get; }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            Stop();

            using (var db = new Database())
            {
                var moduleDescriptions = db.GetCollection<AudioModuleDescription>().FindAll().ToArray();
                foreach (var description in moduleDescriptions)
                {
                    if (Activator.CreateInstance(description.Type) is IAudioModule module)
                    {
                        module.Initialize(description);
                        Modules.Add(module);
                    }
                    else
                    {
                        _logger.Warn($"Could not create an instance of a module. Description-ID: {description.Id}, Type: {description.Type}");
                    }
                }
            }
        }

        public void Stop()
        {
            foreach (var audioModule in Modules)
            {
                audioModule.Dispose();
            }
            Modules.Clear();
        }
    }
}