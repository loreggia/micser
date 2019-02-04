using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Common.Widgets;
using Micser.Engine.Infrastructure;
using Micser.Engine.Infrastructure.DataAccess.Models;
using Micser.Engine.Infrastructure.DataAccess.Repositories;
using Newtonsoft.Json;
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
        private readonly IUnitOfWorkFactory _database;
        private readonly ILogger _logger;
        private readonly List<IAudioModule> _modules;

        public AudioEngine(IUnityContainer container, IUnitOfWorkFactory database, ILogger logger)
        {
            _container = container;
            _database = database;
            _logger = logger;
            _modules = new List<IAudioModule>();
        }

        public void AddModule(long id)
        {
            ModuleDto moduleDto;

            using (var uow = _database.Create())
            {
                var module = uow.GetRepository<IModuleRepository>().Get(id);
                moduleDto = GetModuleDto(module);
            }

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

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            _logger.Info("Starting audio engine");

            Stop();

            using (var uow = _database.Create())
            {
                var moduleDescriptions = uow.GetRepository<IModuleRepository>();

                foreach (var module in moduleDescriptions.GetAll())
                {
                    try
                    {
                        AddModule(module.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Could not load module. ID: {0}", module.Id);
                        moduleDescriptions.Remove(module);
                    }
                }

                var connections = uow.GetRepository<IModuleConnectionRepository>();
                foreach (var connection in connections.GetAll())
                {
                    var source = _modules.FirstOrDefault(m => m.Description.Id == connection.SourceModuleId);
                    var target = _modules.FirstOrDefault(m => m.Description.Id == connection.TargetModuleId);

                    if (source == null)
                    {
                        _logger.Warn($"Source module for connection not found. ID: {connection.SourceModuleId}");
                        continue;
                    }

                    if (target == null)
                    {
                        _logger.Warn($"Target module for connection not found. ID: {connection.TargetModuleId}");
                        continue;
                    }

                    target.Input = source;
                }

                uow.Complete();
            }

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
        }

        public void UpdateModule(long id)
        {
            var audioModule = _modules.SingleOrDefault(m => m.Description.Id == id);
            ModuleDto moduleDto;
            using (var uow = _database.Create())
            {
                var modules = uow.GetRepository<IModuleRepository>();
                var module = modules.Get(id);
                moduleDto = GetModuleDto(module);
            }
            audioModule?.Initialize(moduleDto);
        }

        private static ModuleDto GetModuleDto(Module module)
        {
            return new ModuleDto
            {
                Id = module.Id,
                ModuleState = JsonConvert.DeserializeObject<ModuleState>(module.ModuleStateJson),
                ModuleType = module.ModuleType,
                WidgetState = JsonConvert.DeserializeObject<WidgetState>(module.WidgetStateJson),
                WidgetType = module.WidgetType
            };
        }
    }
}