using Micser.Audio;
using Micser.Audio.Devices;
using Micser.Engine.Configuration;
using Micser.Engine.Contracts;
using Micser.Engine.Modules;

namespace Micser.Engine.Audio;

/// <summary>
/// Owns the audio graph and keeps it, the configuration file and the connected clients in sync. All public members
/// are thread-safe.
/// </summary>
public sealed class AudioHost : IDisposable
{
    private readonly ModuleCatalog _catalog;
    private readonly OrderedDictionary<Guid, ConnectionEntry> _connections = [];
    private readonly AudioDeviceService _devices;
    private readonly Lock _lock = new();
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly OrderedDictionary<Guid, ModuleEntry> _modules = [];
    private readonly IEngineNotifier _notifier;
    private readonly IServiceProvider _services;
    private readonly EngineConfigStore _store;
    private readonly ISystemVolume _systemVolume;
    private readonly List<ConnectionDto> _unavailableConnections = [];
    private readonly List<UnavailableModule> _unavailableModules = [];
    private AudioEngine? _engine;
    private AudioGraph? _graph;
    private IReadOnlyList<ModuleTypeDto>? _moduleTypes;
    private UiPreferencesDto _preferences = new();
    private EngineSettingsDto _settings = new();

    public AudioHost(
        IServiceProvider services,
        ModuleCatalog catalog,
        EngineConfigStore store,
        AudioDeviceService devices,
        ISystemVolume systemVolume,
        IEngineNotifier notifier,
        ILoggerFactory loggerFactory)
    {
        _services = services;
        _catalog = catalog;
        _store = store;
        _devices = devices;
        _systemVolume = systemVolume;
        _notifier = notifier;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AudioHost>();
        _devices.DeviceChanged += OnDeviceChanged;
        _systemVolume.Changed += OnSystemVolumeChanged;
    }

    private AudioEngine Engine => _engine ?? throw new InvalidOperationException("The audio host isn't initialized.");

    private AudioGraph Graph => _graph ?? throw new InvalidOperationException("The audio host isn't initialized.");

    public ModuleDto AddModule(CreateModuleRequest request)
    {
        ThrowIfInvalid(StateValidator.Validate(request));
        var definition = _catalog.TryGetDefinition(request.Type) ?? throw EngineRequestException.Invalid($"Unknown module type '{request.Type}'.");

        lock (_lock)
        {
            var entry = AddModuleCore(Guid.NewGuid(), definition, new ModuleSettings(request.Name, request.Position, 1f, false, false, false, false), null);
            Persist();
            var dto = ToDto(entry);
            _notifier.ModuleChanged(dto);
            return dto;
        }
    }

    public ConnectionDto Connect(CreateConnectionRequest request)
    {
        ThrowIfInvalid(StateValidator.Validate(request));

        lock (_lock)
        {
            var connection = ConnectCore(new ConnectionDto(Guid.NewGuid(), request.SourceModuleId, request.SourcePort, request.TargetModuleId, request.TargetPort));
            Persist();
            _notifier.ConnectionAdded(connection.Dto);
            return connection.Dto;
        }
    }

    public bool Disconnect(Guid connectionId)
    {
        lock (_lock)
        {
            if (!_connections.Remove(connectionId, out var connection))
            {
                return false;
            }

            Graph.Disconnect(connection.Source, connection.Target);
            Persist();
            _notifier.ConnectionRemoved(connectionId);
            return true;
        }
    }

    public void Dispose()
    {
        _devices.DeviceChanged -= OnDeviceChanged;
        _systemVolume.Changed -= OnSystemVolumeChanged;
        lock (_lock)
        {
            TearDown();
        }
    }

    public IReadOnlyList<ConnectionDto> GetConnections()
    {
        lock (_lock)
        {
            return [.. _connections.Values.Select(c => c.Dto)];
        }
    }

    public ModuleDto? GetModule(Guid id)
    {
        lock (_lock)
        {
            return _modules.TryGetValue(id, out var entry) ? ToDto(entry) : null;
        }
    }

    /// <summary>
    /// Returns the live data of a module that provides it (see <see cref="IModuleDataSource"/>), or null.
    /// </summary>
    public object? GetModuleData(Guid id)
    {
        IModuleDataSource? source;
        lock (_lock)
        {
            source = _modules.TryGetValue(id, out var entry) ? entry.Module as IModuleDataSource : null;
        }

        return source?.GetData();
    }

    public IReadOnlyList<ModuleDto> GetModules()
    {
        lock (_lock)
        {
            return [.. _modules.Values.Select(ToDto)];
        }
    }

    /// <summary>
    /// The registered module types with their ports and default states.
    /// </summary>
    public IReadOnlyList<ModuleTypeDto> GetModuleTypes()
    {
        return _moduleTypes ??= [.. _catalog.Definitions.Select(definition =>
        {
            using var module = definition.CreateModule(_services);
            return new ModuleTypeDto(
                definition.Type,
                [.. module.Inputs.Select(p => p.Name)],
                [.. module.Outputs.Select(p => p.Name)],
                definition.GetState(module),
                module is EffectModule);
        })];
    }

    public UiPreferencesDto GetPreferences()
    {
        lock (_lock)
        {
            return _preferences;
        }
    }

    public EngineStatusDto GetStatus()
    {
        lock (_lock)
        {
            return CreateStatus();
        }
    }

    /// <summary>
    /// Loads the configuration, builds the graph and starts processing.
    /// </summary>
    public void Initialize()
    {
        var configuration = _store.Load();

        lock (_lock)
        {
            var settingsErrors = StateValidator.Validate(configuration.Settings);
            if (settingsErrors.Count > 0)
            {
                _logger.LogWarning("Invalid engine settings in the configuration; using the defaults.");
            }

            _settings = settingsErrors.Count == 0 ? configuration.Settings : new EngineSettingsDto();
            _preferences = configuration.Preferences;

            // modules whose plugin isn't loaded and their connections stay in the configuration only
            var unavailableIds = configuration.UnavailableModules.Select(m => m.Id).ToHashSet();
            _unavailableModules.AddRange(configuration.UnavailableModules);
            _unavailableConnections.AddRange(configuration.Connections.Where(c => unavailableIds.Contains(c.SourceModuleId) || unavailableIds.Contains(c.TargetModuleId)));

            Build(configuration.Modules, configuration.Connections.Except(_unavailableConnections));
            Engine.Start();
        }
    }

    /// <summary>
    /// Reads the levels of all modules (see <see cref="AudioModule.ReadLevels"/>) and resets their peaks. Modules that weren't processed
    /// since the previous call are left out. Call from one thread only.
    /// </summary>
    public Dictionary<Guid, PortLevelsDto[]> ReadLevels()
    {
        (Guid Id, AudioModule Module)[] modules;
        lock (_lock)
        {
            modules = [.. _modules.Values.Select(entry => (entry.Id, entry.Module))];
        }

        var levels = new Dictionary<Guid, PortLevelsDto[]>();
        foreach (var (id, module) in modules)
        {
            PortLevelsDto[] ports = [.. module.ReadLevels()
                .Where(port => port.Channels.Length > 0)
                .Select(port => new PortLevelsDto(port.Port, [.. port.Channels.Select(c => c.Peak)], [.. port.Channels.Select(c => c.Rms)]))];
            if (ports.Length > 0)
            {
                levels[id] = ports;
            }
        }

        return levels;
    }

    public bool RemoveModule(Guid id)
    {
        lock (_lock)
        {
            if (!_modules.Remove(id, out var entry))
            {
                return false;
            }

            var removedConnections = _connections.Values.Where(c => c.Dto.SourceModuleId == id || c.Dto.TargetModuleId == id).ToArray();
            foreach (var connection in removedConnections)
            {
                _connections.Remove(connection.Dto.Id);
            }

            _unavailableConnections.RemoveAll(c => c.SourceModuleId == id || c.TargetModuleId == id);
            Graph.Remove(entry.Module);
            entry.Module.StateChanged -= OnModuleStateChanged;
            entry.Module.Dispose();
            Persist();

            foreach (var connection in removedConnections)
            {
                _notifier.ConnectionRemoved(connection.Dto.Id);
            }

            _notifier.ModuleRemoved(id);
            return true;
        }
    }

    /// <summary>
    /// Rebuilds the graph with the current settings, which reopens all device streams with fresh buffers. The audio pauses briefly.
    /// </summary>
    public EngineStatusDto RestartAudio()
    {
        lock (_lock)
        {
            _logger.LogInformation("Restarting the audio.");
            return Rebuild(_settings);
        }
    }

    public EngineStatusDto Start()
    {
        lock (_lock)
        {
            Engine.Start();
            var status = CreateStatus();
            _notifier.StatusChanged(status);
            return status;
        }
    }

    public EngineStatusDto Stop()
    {
        lock (_lock)
        {
            Engine.Stop();
            var status = CreateStatus();
            _notifier.StatusChanged(status);
            return status;
        }
    }

    /// <summary>
    /// Replaces a module's settings and state. The DTO's type must match the module's type.
    /// </summary>
    public ModuleDto UpdateModule(Guid id, ModuleDto dto)
    {
        ThrowIfInvalid(StateValidator.Validate(dto));

        lock (_lock)
        {
            if (!_modules.TryGetValue(id, out var entry))
            {
                throw EngineRequestException.NotFound($"Module {id} not found.");
            }

            if (_catalog.GetDefinition(dto) != entry.Definition)
            {
                throw EngineRequestException.Invalid($"Module {id} is of type '{entry.Definition.Type}'.");
            }

            entry.Settings = WithSystemVolume(ModuleSettings.From(dto) with { IsBypassed = dto.IsBypassed && entry.Module is EffectModule });
            ApplySettings(entry.Module, entry.Settings);
            entry.Definition.SetState(entry.Module, dto.StateObject);
            Persist();

            var updated = ToDto(entry);
            _notifier.ModuleChanged(updated);
            return updated;
        }
    }

    /// <summary>
    /// Changes the processing format. The graph is rebuilt, which briefly interrupts the audio.
    /// </summary>
    public UiPreferencesDto UpdatePreferences(UiPreferencesDto preferences)
    {
        lock (_lock)
        {
            _preferences = preferences;
            Persist();
            _notifier.PreferencesChanged(preferences);
            return preferences;
        }
    }

    public EngineStatusDto UpdateSettings(EngineSettingsDto settings)
    {
        ThrowIfInvalid(StateValidator.Validate(settings));

        lock (_lock)
        {
            return Rebuild(settings);
        }
    }

    private static void ApplySettings(AudioModule module, ModuleSettings settings)
    {
        module.Volume = settings.Volume;
        module.IsMuted = settings.IsMuted;
        if (module is EffectModule effect)
        {
            effect.IsBypassed = settings.IsBypassed;
        }
    }

    private static void ThrowIfInvalid(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw EngineRequestException.Invalid(errors);
        }
    }

    private ModuleEntry AddModuleCore(Guid id, AudioModuleDefinition definition, ModuleSettings settings, object? state)
    {
        settings = WithSystemVolume(settings);
        var module = definition.CreateModule(_services);
        try
        {
            ApplySettings(module, settings);
            if (state != null)
            {
                definition.SetState(module, state);
            }

            Graph.Add(module);
        }
        catch
        {
            module.Dispose();
            throw;
        }

        module.StateChanged += OnModuleStateChanged;
        var entry = new ModuleEntry(id, definition, module, settings with { IsBypassed = settings.IsBypassed && module is EffectModule });
        _modules.Add(id, entry);
        return entry;
    }

    /// <summary>
    /// Creates the graph with the current settings and adds the modules and connections, skipping invalid ones.
    /// </summary>
    private void Build(IEnumerable<ModuleDto> modules, IEnumerable<ConnectionDto> connections)
    {
        _graph = new AudioGraph(new ProcessingFormat(_settings.SampleRate, _settings.FrameCount), _loggerFactory.CreateLogger<AudioGraph>());
        _engine = new AudioEngine(_graph, _loggerFactory.CreateLogger<AudioEngine>());

        foreach (var dto in modules)
        {
            var definition = _catalog.GetDefinition(dto);
            var errors = StateValidator.Validate(dto);
            if (errors.Count > 0 || _modules.ContainsKey(dto.Id))
            {
                _logger.LogWarning("Skipping invalid module {Id} ({Type}): {Errors}", dto.Id, definition.Type, errors);
                continue;
            }

            try
            {
                AddModuleCore(dto.Id, definition, ModuleSettings.From(dto), dto.StateObject);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping module {Id} ({Type}).", dto.Id, definition.Type);
            }
        }

        foreach (var connection in connections)
        {
            try
            {
                ConnectCore(connection);
            }
            catch (EngineRequestException ex)
            {
                _logger.LogWarning("Skipping connection {Id}: {Reason}", connection.Id, ex.Message);
            }
        }
    }

    private ConnectionEntry ConnectCore(ConnectionDto dto)
    {
        if (!_modules.TryGetValue(dto.SourceModuleId, out var source) || !_modules.TryGetValue(dto.TargetModuleId, out var target))
        {
            throw EngineRequestException.Invalid("Source or target module not found.");
        }

        var sourcePort = source.Module.Outputs.FirstOrDefault(p => p.Name == dto.SourcePort)
            ?? throw EngineRequestException.Invalid($"Module type '{source.Definition.Type}' has no output '{dto.SourcePort}'.");
        var targetPort = target.Module.Inputs.FirstOrDefault(p => p.Name == dto.TargetPort)
            ?? throw EngineRequestException.Invalid($"Module type '{target.Definition.Type}' has no input '{dto.TargetPort}'.");

        if (_connections.ContainsKey(dto.Id))
        {
            throw EngineRequestException.Conflict($"Connection {dto.Id} already exists.");
        }

        try
        {
            Graph.Connect(sourcePort, targetPort);
        }
        catch (InvalidOperationException ex)
        {
            throw EngineRequestException.Conflict(ex.Message);
        }

        var entry = new ConnectionEntry(dto, sourcePort, targetPort);
        _connections.Add(dto.Id, entry);
        return entry;
    }

    private EngineStatusDto CreateStatus()
    {
        var statistics = Engine.Statistics;
        return new EngineStatusDto(Engine.IsRunning, _settings, statistics.Blocks, statistics.LateBlocks, statistics.MaxProcessingTime.TotalMilliseconds);
    }

    private void OnDeviceChanged(object? sender, AudioDeviceChangedEventArgs e)
    {
        _notifier.DevicesChanged();
    }

    private void OnModuleStateChanged(object? sender, EventArgs e)
    {
        // Modules raise this while holding their own locks, which the host can't take while holding its lock.
        ThreadPool.QueueUserWorkItem(_ =>
        {
            lock (_lock)
            {
                var entry = _modules.Values.FirstOrDefault(m => m.Module == sender);
                if (entry != null)
                {
                    Persist();
                    _notifier.ModuleChanged(ToDto(entry));
                }
            }
        });
    }

    private void OnSystemVolumeChanged(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            var changed = new List<ModuleEntry>();
            foreach (var entry in _modules.Values)
            {
                var settings = WithSystemVolume(entry.Settings);
                if (settings != entry.Settings)
                {
                    entry.Settings = settings;
                    ApplySettings(entry.Module, settings);
                    changed.Add(entry);
                }
            }

            if (changed.Count > 0)
            {
                Persist();
                foreach (var entry in changed)
                {
                    _notifier.ModuleChanged(ToDto(entry));
                }
            }
        }
    }

    private void Persist()
    {
        _store.Save(new EngineConfiguration
        {
            Settings = _settings,
            Preferences = _preferences,
            Modules = [.. _modules.Values.Select(ToDto)],
            UnavailableModules = [.. _unavailableModules],
            Connections = [.. _connections.Values.Select(c => c.Dto), .. _unavailableConnections],
        });
    }

    /// <summary>
    /// Tears the graph down and builds it again with <paramref name="settings"/>. Call while holding the lock.
    /// </summary>
    private EngineStatusDto Rebuild(EngineSettingsDto settings)
    {
        var modules = _modules.Values.Select(ToDto).ToArray();
        var connections = _connections.Values.Select(c => c.Dto).ToArray();
        var wasRunning = Engine.IsRunning;

        TearDown();
        _settings = settings;
        Build(modules, connections);
        if (wasRunning)
        {
            Engine.Start();
        }

        Persist();
        var status = CreateStatus();
        _notifier.StatusChanged(status);
        return status;
    }

    private void TearDown()
    {
        _engine?.Stop();
        foreach (var entry in _modules.Values)
        {
            entry.Module.StateChanged -= OnModuleStateChanged;
            entry.Module.Dispose();
        }

        _modules.Clear();
        _connections.Clear();
    }

    private ModuleDto ToDto(ModuleEntry entry)
    {
        return _catalog.CreateDto(entry.Id, entry.Definition, entry.Settings, entry.Definition.GetState(entry.Module));
    }

    /// <summary>
    /// Takes volume and mute from the system volume if the module follows it and there is a default output device.
    /// </summary>
    private ModuleSettings WithSystemVolume(ModuleSettings settings)
    {
        return settings.UseSystemVolume && _systemVolume.Level is { } level
            ? settings with { Volume = level.Gain, IsMuted = level.IsMuted }
            : settings;
    }

    private sealed record ConnectionEntry(ConnectionDto Dto, OutputPort Source, InputPort Target);

    private sealed class ModuleEntry
    {
        public ModuleEntry(Guid id, AudioModuleDefinition definition, AudioModule module, ModuleSettings settings)
        {
            Id = id;
            Definition = definition;
            Module = module;
            Settings = settings;
        }

        public AudioModuleDefinition Definition { get; }

        public Guid Id { get; }

        public AudioModule Module { get; }

        public ModuleSettings Settings { get; set; }
    }
}
