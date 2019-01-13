using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Engine.Infrastructure;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity;

namespace Micser.Engine.Audio
{
    public sealed class AudioEngine : IAudioEngine
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

        public void Start(IUnityContainer container)
        {
            Stop();

            var database = container.Resolve<IDatabase>();
            var db = database.GetContext();

            var moduleDescriptions = db.GetCollection<ModuleDescription>().ToArray();
            foreach (var description in moduleDescriptions)
            {
                var type = Type.GetType(description.Type);
                if (type != null)
                {
                    if (container.Resolve(type) is IAudioModule module)
                    {
                        module.Initialize(description);
                        Modules.Add(module);
                    }
                    else
                    {
                        _logger.Warn(
                            $"Could not create an instance of a module. Description-ID: {description.Id}, Type: {description.Type}");
                    }
                }
                else
                {
                    _logger.Warn(
                        $"Could not find module type. Description-ID: {description.Id}, Type: {description.Type}");
                }
            }

            var connections = db.GetCollection<ModuleConnectionDescription>().ToArray();
            foreach (var connection in connections)
            {
                var source = Modules.FirstOrDefault(m => m.Description.Id == connection.SourceId);
                var target = Modules.FirstOrDefault(m => m.Description.Id == connection.TargetId);

                if (source == null)
                {
                    _logger.Warn($"Source module for connection not found. ID: {connection.SourceId}");
                    continue;
                }

                if (target == null)
                {
                    _logger.Warn($"Target module for connection not found. ID: {connection.TargetId}");
                    continue;
                }

                target.Input = source;
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