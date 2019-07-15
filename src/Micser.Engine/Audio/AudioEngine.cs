using CSCore.CoreAudioAPI;
using Micser.Common.Api;
using Micser.Common.Audio;
using Micser.Common.Settings;
using Micser.Engine.Infrastructure.Audio;
using Micser.Engine.Infrastructure.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity;

namespace Micser.Engine.Audio
{
    public sealed class AudioEngine : IAudioEngine
    {
        private readonly IApiServer _apiServer;
        private readonly IUnityContainer _container;
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly AudioEndpointVolumeCallback _endpointVolumeCallback;
        private readonly ILogger _logger;
        private readonly IModuleConnectionService _moduleConnectionService;
        private readonly List<IAudioModule> _modules;
        private readonly IModuleService _moduleService;
        private readonly ISettingsService _settingsService;
        private AudioEndpointVolume _endpointVolume;

        public AudioEngine(
            // todo create audio module factory
            IUnityContainer container,
            ILogger logger,
            IModuleService moduleService,
            IModuleConnectionService moduleConnectionService,
            IApiServer apiServer,
            ISettingsService settingsService)
        {
            _container = container;
            _logger = logger;
            _moduleService = moduleService;
            _moduleConnectionService = moduleConnectionService;
            _apiServer = apiServer;
            _settingsService = settingsService;
            _modules = new List<IAudioModule>();
            _deviceEnumerator = new MMDeviceEnumerator();
            _endpointVolumeCallback = new AudioEndpointVolumeCallback();
        }

        public bool IsRunning { get; private set; }

        public void AddConnection(long id)
        {
            var connectionDto = _moduleConnectionService.GetById(id);
            var source = _modules.FirstOrDefault(m => m.Id == connectionDto.SourceId);
            var target = _modules.FirstOrDefault(m => m.Id == connectionDto.TargetId);

            if (source != null && target != null)
            {
                source.AddOutput(target);
            }
        }

        public void AddModule(long id)
        {
            var moduleDto = _moduleService.GetById(id);

            var type = Type.GetType(moduleDto.ModuleType);
            if (type != null)
            {
                if (_container.Resolve(type) is IAudioModule audioModule)
                {
                    audioModule.Id = id;
                    audioModule.SetState(moduleDto.State);
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
            if (_endpointVolumeCallback != null)
            {
                _endpointVolumeCallback.NotifyRecived -= VolumeNotifyReceived;
            }

            Stop();

            _deviceEnumerator?.Dispose();
        }

        public void RemoveConnection(long id)
        {
            var connectionDto = _moduleConnectionService.GetById(id);
            var source = _modules.FirstOrDefault(m => m.Id == connectionDto.SourceId);
            var target = _modules.FirstOrDefault(m => m.Id == connectionDto.TargetId);

            if (source != null && target != null)
            {
                source.RemoveOutput(target);
            }
        }

        public void RemoveModule(long id)
        {
            var audioModule = _modules.SingleOrDefault(m => m.Id == id);
            if (audioModule != null)
            {
                _modules.Remove(audioModule);
                audioModule.Dispose();
            }
        }

        public void Start()
        {
            _logger.Info("Starting audio engine");

            Stop();

            _deviceEnumerator.DefaultDeviceChanged += DefaultDeviceChanged;
            _endpointVolumeCallback.NotifyRecived += VolumeNotifyReceived;

            SetupDefaultEndpoint();

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
                var source = _modules.FirstOrDefault(m => m.Id == connection.SourceId);
                var target = _modules.FirstOrDefault(m => m.Id == connection.TargetId);

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

                source.AddOutput(target);
            }

            IsRunning = true;
            _logger.Info("Audio engine started");
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            _logger.Info("Stopping audio engine");

            _deviceEnumerator.DefaultDeviceChanged -= DefaultDeviceChanged;
            _endpointVolumeCallback.NotifyRecived -= VolumeNotifyReceived;

            _endpointVolume?.UnregisterControlChangeNotify(_endpointVolumeCallback);
            _endpointVolume?.Dispose();

            _logger.Info($"Disposing modules ({_modules?.Count ?? 0})");

            if (_modules?.Count > 0)
            {
                foreach (var audioModule in _modules)
                {
                    _logger.Info($"Disposing module {audioModule.Id} ({audioModule.GetType()})");
                    audioModule.Dispose();
                }

                _modules.Clear();
            }

            _logger.Info("Audio engine stopped");

            IsRunning = false;
        }

        public void UpdateModule(long id)
        {
            var audioModule = _modules.SingleOrDefault(m => m.Id == id);
            if (audioModule != null)
            {
                var moduleDto = _moduleService.GetById(id);

                var applySystemVolume = !audioModule.UseSystemVolume && moduleDto.State.UseSystemVolume;
                if (applySystemVolume && _endpointVolume != null)
                {
                    moduleDto.State.IsMuted = _endpointVolume.IsMuted;
                    moduleDto.State.Volume = _endpointVolume.MasterVolumeLevelScalar;
                    _moduleService.Update(moduleDto);
                    _apiServer.SendMessageAsync(new JsonRequest("modules", "updatevolume", moduleDto));
                }

                audioModule.SetState(moduleDto.State);
            }
        }

        private async void DefaultDeviceChanged(object sender, DefaultDeviceChangedEventArgs e)
        {
            if (!IsRunning)
            {
                return;
            }

            await Task.Run(SetupDefaultEndpoint).ConfigureAwait(false);
        }

        private void SetupDefaultEndpoint()
        {
            if (_endpointVolume != null)
            {
                _endpointVolume.UnregisterControlChangeNotify(_endpointVolumeCallback);
                _endpointVolume.Dispose();
            }

            try
            {
                var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                _endpointVolume = AudioEndpointVolume.FromDevice(defaultDevice);
                _endpointVolume.RegisterControlChangeNotify(_endpointVolumeCallback);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private async void VolumeNotifyReceived(object sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            await Task.Run(() =>
            {
                var modules = _modules.OfType<AudioModule>().Where(m => m.UseSystemVolume).ToArray();
                foreach (var module in modules)
                {
                    module.Volume = e.MasterVolume;
                    module.IsMuted = e.IsMuted;

                    var moduleDto = _moduleService.GetById(module.Id);

                    if (moduleDto.State == null)
                    {
                        moduleDto.State = module.GetState();
                    }

                    moduleDto.State.UseSystemVolume = true;
                    moduleDto.State.IsMuted = e.IsMuted;
                    moduleDto.State.Volume = e.MasterVolume;

                    _moduleService.Update(moduleDto);

                    _apiServer.SendMessageAsync(new JsonRequest("modules", "updatevolume", moduleDto));
                }
            }).ConfigureAwait(false);
        }
    }
}