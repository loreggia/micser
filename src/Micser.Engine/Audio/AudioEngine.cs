using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity;

namespace Micser.Engine.Audio
{
    public sealed class AudioEngine : IAudioEngine
    {
        private readonly IUnityContainer _container;
        private readonly ILogger _logger;
        private readonly IModuleConnectionService _moduleConnectionService;
        private readonly List<IAudioModule> _modules;
        private readonly IModuleService _moduleService;

        public AudioEngine(IUnityContainer container, ILogger logger, IModuleService moduleService, IModuleConnectionService moduleConnectionService)
        {
            _container = container;
            _logger = logger;
            _moduleService = moduleService;
            _moduleConnectionService = moduleConnectionService;
            _modules = new List<IAudioModule>();
        }

        public bool IsRunning { get; private set; }

        public void AddModule(long id)
        {
            var moduleDto = _moduleService.GetById(id);

            var type = Type.GetType(moduleDto.ModuleType);
            if (type != null)
            {
                if (_container.Resolve(type) is IAudioModule audioModule)
                {
                    audioModule.Initialize(moduleDto);
                    _modules.Add(audioModule);
                }
                else
                {
                    _logger.Warn(
                        $"Could not create an instance of a module. ID: {moduleDto.Id}, Type: {moduleDto.ModuleType}");
                }
            }
            else
            {
                _logger.Warn(
                    $"Could not find module type. ID: {moduleDto.Id}, Type: {moduleDto.ModuleType}");
            }
        }

        public void DeleteModule(long id)
        {
            var audioModule = _modules.SingleOrDefault(m => m.Description.Id == id);
            if (audioModule != null)
            {
                _modules.Remove(audioModule);
                audioModule.Dispose();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            _logger.Info("Starting audio engine");

            Stop();

            foreach (var module in _moduleService.GetAll())
            {
                try
                {
                    AddModule(module.Id);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Could not load module. ID: {0}", module.Id);
                    _moduleService.Delete(module.Id);
                }
            }

            foreach (var connection in _moduleConnectionService.GetAll())
            {
                var source = _modules.FirstOrDefault(m => m.Description.Id == connection.SourceId);
                var target = _modules.FirstOrDefault(m => m.Description.Id == connection.TargetId);

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

            IsRunning = true;
            _logger.Info("Audio engine started");
        }

        public void Stop()
        {
            if (_modules.Count == 0)
            {
                return;
            }

            _logger.Info("Stopping audio engine");

            foreach (var audioModule in _modules)
            {
                audioModule.Dispose();
            }

            _modules.Clear();

            _logger.Info("Audio engine stopped");

            IsRunning = false;
        }

        public void UpdateModule(long id)
        {
            var audioModule = _modules.SingleOrDefault(m => m.Description.Id == id);
            var moduleDto = _moduleService.GetById(id);
            audioModule?.Initialize(moduleDto);
        }
    }
}