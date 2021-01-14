using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSCore.CoreAudioAPI;
using Microsoft.Extensions.Logging;
using Micser.Common.Audio;
using Micser.Common.Extensions;
using Micser.Common.Services;

namespace Micser.Audio
{
    public sealed class AudioEngine : IAudioEngine
    {
        private readonly IServiceProvider _container;
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly AudioEndpointVolumeCallback _endpointVolumeCallback;
        private readonly ILogger<AudioEngine> _logger;
        private readonly IModuleConnectionService _moduleConnectionService;
        private readonly List<IAudioModule> _modules;
        private readonly IModuleService _moduleService;
        private AudioEndpointVolume? _endpointVolume;

        public AudioEngine(
            // todo create audio module factory
            IServiceProvider container,
            ILogger<AudioEngine> logger,
            IModuleService moduleService,
            IModuleConnectionService moduleConnectionService)
        {
            _container = container;
            _logger = logger;
            _moduleService = moduleService;
            _moduleConnectionService = moduleConnectionService;
            _modules = new List<IAudioModule>();
            _deviceEnumerator = new MMDeviceEnumerator();
            _endpointVolumeCallback = new AudioEndpointVolumeCallback();
        }

        public bool IsRunning { get; private set; }

        public async Task AddConnectionAsync(long id)
        {
            var connectionDto = await _moduleConnectionService.GetByIdAsync(id).ConfigureAwait(false);

            if (connectionDto != null)
            {
                var source = _modules.FirstOrDefault(m => m.Id == connectionDto.SourceId);
                var target = _modules.FirstOrDefault(m => m.Id == connectionDto.TargetId);

                if (source != null && target != null)
                {
                    source.AddOutput(target);
                }
            }
        }

        public async Task AddModuleAsync(long id)
        {
            var moduleDto = await _moduleService.GetByIdAsync(id).ConfigureAwait(false);

            if (moduleDto == null)
            {
                _logger.LogWarning($"Module with ID {id} not found.");
                return;
            }

            var type = Type.GetType(moduleDto.Type);
            if (type != null)
            {
                if (_container.GetService(type) is IAudioModule audioModule)
                {
                    audioModule.Id = id;
                    audioModule.SetState(moduleDto.State);
                    _modules.Add(audioModule);
                }
                else
                {
                    _logger.LogWarning($"Could not create an instance of a module. ID: {moduleDto.Id}, Type: {moduleDto.Type}");
                }
            }
            else
            {
                _logger.LogWarning($"Could not find module type. ID: {moduleDto.Id}, Type: {moduleDto.Type}");
            }
        }

        public void Dispose()
        {
            _endpointVolumeCallback.NotifyRecived -= VolumeNotifyReceived;

            StopAsync().GetAwaiter().GetResult();

            _deviceEnumerator.Dispose();
        }

        public async Task RemoveConnectionAsync(long id)
        {
            var connectionDto = await _moduleConnectionService.GetByIdAsync(id).ConfigureAwait(false);

            if (connectionDto == null)
            {
                return;
            }

            var source = _modules.FirstOrDefault(m => m.Id == connectionDto.SourceId);
            var target = _modules.FirstOrDefault(m => m.Id == connectionDto.TargetId);

            if (source != null && target != null)
            {
                source.RemoveOutput(target);
            }
        }

        public Task RemoveModuleAsync(long id)
        {
            var audioModule = _modules.SingleOrDefault(m => m.Id == id);

            if (audioModule != null)
            {
                _modules.Remove(audioModule);
                audioModule.Dispose();
            }

            return Task.CompletedTask;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Starting audio engine");

            await StopAsync().ConfigureAwait(false);

            _deviceEnumerator.DefaultDeviceChanged += DefaultDeviceChanged;
            _endpointVolumeCallback.NotifyRecived += VolumeNotifyReceived;

            SetupDefaultEndpoint();

            await foreach (var module in _moduleService.GetAllAsync())
            {
                try
                {
                    await AddModuleAsync(module.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not load module. ID: {0}", module.Id);
                    await _moduleService.DeleteAsync(module.Id).ConfigureAwait(false);
                }
            }

            await foreach (var connection in _moduleConnectionService.GetAllAsync())
            {
                var source = _modules.FirstOrDefault(m => m.Id == connection.SourceId);
                var target = _modules.FirstOrDefault(m => m.Id == connection.TargetId);

                if (source == null)
                {
                    _logger.LogWarning($"Source module for connection not found. ID: {connection.SourceId}");
                    continue;
                }

                if (target == null)
                {
                    _logger.LogWarning($"Target module for connection not found. ID: {connection.TargetId}");
                    continue;
                }

                source.AddOutput(target);
            }

            IsRunning = true;
            _logger.LogInformation("Audio engine started");
        }

        public Task StopAsync()
        {
            if (!IsRunning)
            {
                return Task.CompletedTask;
            }

            _logger.LogInformation("Stopping audio engine");

            _deviceEnumerator.DefaultDeviceChanged -= DefaultDeviceChanged;
            _endpointVolumeCallback.NotifyRecived -= VolumeNotifyReceived;

            _endpointVolume?.UnregisterControlChangeNotify(_endpointVolumeCallback);
            _endpointVolume?.Dispose();

            _logger.LogInformation($"Disposing modules ({_modules.Count})");

            if (_modules.Count > 0)
            {
                foreach (var audioModule in _modules)
                {
                    _logger.LogInformation($"Disposing module {audioModule.Id} ({audioModule.GetType()})");
                    audioModule.Dispose();
                }

                _modules.Clear();
            }

            _logger.LogInformation("Audio engine stopped");

            IsRunning = false;

            return Task.CompletedTask;
        }

        public async Task UpdateModuleAsync(long id)
        {
            var audioModule = _modules.SingleOrDefault(m => m.Id == id);
            if (audioModule != null)
            {
                var moduleDto = await _moduleService.GetByIdAsync(id).ConfigureAwait(false);

                if (moduleDto == null)
                {
                    _logger.LogWarning($"Module with ID {id} not found.");
                    return;
                }

                var applySystemVolume = !audioModule.UseSystemVolume && moduleDto.State.UseSystemVolume;
                if (applySystemVolume && _endpointVolume != null)
                {
                    moduleDto.State.IsMuted = _endpointVolume.IsMuted;
                    moduleDto.State.Volume = _endpointVolume.MasterVolumeLevelScalar;

                    await _moduleService.UpdateAsync(moduleDto).ConfigureAwait(false);
                    //todo
                    //_engineEventService.SendMessageAsync(new EngineEvent { Type = "VolumeChanged", Content = moduleDto.Id.ToString(CultureInfo.InvariantCulture) });
                }

                audioModule.SetState(moduleDto.State);
            }
        }

        private async void DefaultDeviceChanged(object? sender, DefaultDeviceChangedEventArgs e)
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
                _logger.LogError(ex, "Failed to set up default endpoint callbacks.");
            }
        }

        private async void VolumeNotifyReceived(object? sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            await Task.Run(async () =>
            {
                var modules = _modules.OfType<AudioModule>().Where(m => m.UseSystemVolume).ToArray();
                foreach (var module in modules)
                {
                    module.Volume = e.MasterVolume;
                    module.IsMuted = e.IsMuted;

                    var moduleDto = await _moduleService.GetByIdAsync(module.Id).ConfigureAwait(false);

                    if (moduleDto == null)
                    {
                        _logger.LogWarning($"Module with ID {module.Id} not found.");
                        return;
                    }

                    moduleDto.State.AddRange(module.GetState());
                    moduleDto.State.UseSystemVolume = true;
                    moduleDto.State.IsMuted = e.IsMuted;
                    moduleDto.State.Volume = e.MasterVolume;

                    await _moduleService.UpdateAsync(moduleDto).ConfigureAwait(false);
                    //todo
                    //_engineEventService.SendMessageAsync(new EngineEvent { Type = "VolumeChanged", Content = moduleDto.Id.ToString(CultureInfo.InvariantCulture) });
                }
            }).ConfigureAwait(false);
        }
    }
}