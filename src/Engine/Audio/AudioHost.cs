using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Micser.Audio;
using Micser.Audio.Devices;
using Micser.Engine.Configuration;
using Micser.Engine.Contracts;
using Micser.Engine.Modules;
using Micser.Engine.Plugins;

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
    private readonly EngineOptions _options;
    private readonly PluginCatalog _plugins;
    private readonly IServiceProvider _services;
    private readonly EngineConfigStore _store;
    private readonly OrderedDictionary<Guid, SubgraphDto> _subgraphs = [];
    private readonly ISystemVolume _systemVolume;
    private readonly OrderedDictionary<Guid, SubgraphTemplate> _templates = [];
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
        PluginCatalog plugins,
        IOptions<EngineOptions> options,
        ILoggerFactory loggerFactory
    )
    {
        _services = services;
        _catalog = catalog;
        _plugins = plugins;
        _options = options.Value;
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
        var definition =
            _catalog.TryGetDefinition(request.Type)
            ?? throw EngineRequestException.Invalid($"Unknown module type '{request.Type}'.");

        lock (_lock)
        {
            ThrowIfUnknownSubgraph(request.SubgraphId);
            var settings = new ModuleSettings(
                request.Name,
                request.Position,
                1f,
                false,
                false,
                false,
                false,
                request.SubgraphId,
                ShowChannels: request.ShowChannels
            );
            var entry = AddModuleCore(Guid.NewGuid(), definition, settings, null);
            Persist();
            var dto = ToDto(entry);
            _notifier.ModuleChanged(dto);
            return dto;
        }
    }

    /// <summary>
    /// Creates a subgraph and moves the modules into it, converting their positions to be relative to it.
    /// </summary>
    public SubgraphDto AddSubgraph(CreateSubgraphRequest request)
    {
        ThrowIfInvalid(StateValidator.Validate(request));

        lock (_lock)
        {
            var members = request
                .ModuleIds.Distinct()
                .Select(id =>
                    _modules.TryGetValue(id, out var entry)
                        ? entry
                        : throw EngineRequestException.Invalid($"Module {id} not found.")
                )
                .ToArray();

            var subgraph = new SubgraphDto(Guid.NewGuid(), request.Name, request.Position, request.Size, request.Color);
            var positions = members
                .Select(m => Offset(GetAbsolutePosition(m.Settings), -subgraph.Position.X, -subgraph.Position.Y))
                .ToArray();
            _subgraphs.Add(subgraph.Id, subgraph);
            for (var i = 0; i < members.Length; i++)
            {
                members[i].Settings = members[i].Settings with
                {
                    SubgraphId = subgraph.Id,
                    Position = positions[i],
                    TemplateModuleId = null,
                };
                ApplySettings(members[i].Module, members[i].Settings);
            }

            Persist();
            _notifier.SubgraphChanged(subgraph);
            foreach (var entry in members)
            {
                _notifier.ModuleChanged(ToDto(entry));
            }

            return subgraph;
        }
    }

    public ConnectionDto Connect(CreateConnectionRequest request)
    {
        ThrowIfInvalid(StateValidator.Validate(request));

        lock (_lock)
        {
            var dto = new ConnectionDto(
                Guid.NewGuid(),
                request.SourceModuleId,
                request.SourcePort,
                request.TargetModuleId,
                request.TargetPort,
                request.SourceChannel,
                request.TargetChannel
            );
            if (
                dto.TargetChannel is { } channel
                && _modules.TryGetValue(dto.TargetModuleId, out var target)
                && target.Settings.ChannelCount is { } channelCount
                && channel >= channelCount
                && IsFlexibleInput(target, dto.TargetPort)
            )
            {
                throw EngineRequestException.Invalid(
                    new Dictionary<string, string[]>
                    {
                        ["targetChannel"] = [$"The module has {channelCount} channels."],
                    }
                );
            }

            var connection = ConnectCore(dto);
            ModuleEntry[] changed =
            [
                .. new[] { _modules[dto.SourceModuleId], _modules[dto.TargetModuleId] }
                    .Distinct()
                    .Where(FixChannelSettings),
            ];
            Persist();
            foreach (var entry in changed)
            {
                _notifier.ModuleChanged(ToDto(entry));
            }

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

            Disconnect(connection);
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

    /// <summary>
    /// The port layouts of the modules whose layouts differ from <paramref name="known"/> or aren't in it. It holds each module's layouts
    /// (inputs, then outputs) and is updated to the current ones; modules no longer in the graph are removed from it.
    /// </summary>
    public IReadOnlyList<ModulePortLayoutsDto> GetChangedPortLayouts(Dictionary<Guid, ChannelLayout[]> known)
    {
        List<ModulePortLayoutsDto>? changed = null;
        lock (_lock)
        {
            foreach (var entry in _modules.Values)
            {
                var module = entry.Module;
                var isChanged = false;
                if (!known.TryGetValue(entry.Id, out var layouts))
                {
                    layouts = new ChannelLayout[module.Inputs.Count + module.Outputs.Count];
                    known.Add(entry.Id, layouts);
                    isChanged = true;
                }

                for (var i = 0; i < layouts.Length; i++)
                {
                    var layout =
                        i < module.Inputs.Count
                            ? module.Inputs[i].LastLayout
                            : module.Outputs[i - module.Inputs.Count].LastLayout;
                    if (layout != layouts[i])
                    {
                        layouts[i] = layout;
                        isChanged = true;
                    }
                }

                if (isChanged)
                {
                    (changed ??= []).Add(ToPortLayoutsDto(entry.Id, module));
                }
            }

            if (known.Count > _modules.Count)
            {
                foreach (var id in known.Keys.Where(id => !_modules.ContainsKey(id)).ToArray())
                {
                    known.Remove(id);
                }
            }
        }

        return changed ?? [];
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
        return _moduleTypes ??= [
            .. _catalog.Definitions.Select(definition =>
            {
                using var module = definition.CreateModule(_services);
                return new ModuleTypeDto(
                    definition.Type,
                    [.. module.Inputs.Select(p => p.Name)],
                    [.. module.Outputs.Select(p => p.Name)],
                    definition.GetState(module),
                    module is EffectModule,
                    SupportsChannelCount(module),
                    [.. module.Inputs.Where(p => p.Layout == null).Select(p => p.Name)]
                );
            }),
        ];
    }

    /// <summary>
    /// The layouts of all modules' ports in the last processed block.
    /// </summary>
    public IReadOnlyList<ModulePortLayoutsDto> GetPortLayouts()
    {
        (Guid Id, AudioModule Module)[] modules;
        lock (_lock)
        {
            modules = [.. _modules.Values.Select(entry => (entry.Id, entry.Module))];
        }

        return [.. modules.Select(m => ToPortLayoutsDto(m.Id, m.Module))];
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

    public IReadOnlyList<SubgraphDto> GetSubgraphs()
    {
        lock (_lock)
        {
            return [.. _subgraphs.Values];
        }
    }

    public IReadOnlyList<SubgraphTemplateDto> GetTemplates()
    {
        lock (_lock)
        {
            return [.. _templates.Values.Select(ToDto)];
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
            if (_options.LoadPluginTemplates)
            {
                LoadPluginTemplates();
            }

            foreach (var template in configuration.Templates)
            {
                if (
                    string.IsNullOrWhiteSpace(template.Name)
                    || !_templates.TryAdd(template.Id, template with { IsBuiltIn = false })
                )
                {
                    _logger.LogWarning("Skipping invalid subgraph template {Id}.", template.Id);
                }
            }

            var renamedTemplates = RenameTemplatesNamedLikeBuiltIns();

            foreach (var subgraph in configuration.Subgraphs)
            {
                // a missing template's reference is kept: it may be a built-in one whose plugin isn't loaded (removing a template clears them)
                var errors = StateValidator.Validate(subgraph);
                if (errors.Count > 0 || !_subgraphs.TryAdd(subgraph.Id, subgraph))
                {
                    _logger.LogWarning("Skipping invalid subgraph {Id}: {Errors}", subgraph.Id, errors);
                }
            }

            // modules whose plugin isn't loaded and their connections stay in the configuration only
            var unavailableIds = configuration.UnavailableModules.Select(m => m.Id).ToHashSet();
            _unavailableModules.AddRange(configuration.UnavailableModules);
            _unavailableConnections.AddRange(
                configuration.Connections.Where(c =>
                    unavailableIds.Contains(c.SourceModuleId) || unavailableIds.Contains(c.TargetModuleId)
                )
            );

            var modules = configuration.Modules.Select(m =>
                m.SubgraphId is { } id && !_subgraphs.ContainsKey(id) ? m with { SubgraphId = null } : m
            );
            Build(modules, configuration.Connections.Except(_unavailableConnections));
            if (renamedTemplates)
            {
                Persist();
            }

            Engine.Start();
        }
    }

    /// <summary>
    /// Creates a subgraph from a template, with new modules and the connections between them.
    /// </summary>
    public SubgraphDto InstantiateTemplate(Guid templateId, InstantiateSubgraphTemplateRequest request)
    {
        ThrowIfInvalid(StateValidator.Validate(request));

        lock (_lock)
        {
            var template = GetUsableTemplate(templateId);
            var subgraph = new SubgraphDto(
                Guid.NewGuid(),
                template.Name,
                request.Position,
                template.Size,
                template.Color,
                TemplateId: template.Id,
                TemplateRevision: template.Revision
            );
            _subgraphs.Add(subgraph.Id, subgraph);

            var moduleIds = new Dictionary<Guid, Guid>();
            var modules = new List<ModuleEntry>();
            foreach (var dto in template.Modules)
            {
                var settings = ModuleSettings.From(dto) with { SubgraphId = subgraph.Id, TemplateModuleId = dto.Id };
                var entry = AddModuleCore(Guid.NewGuid(), _catalog.GetDefinition(dto), settings, dto.StateObject);
                moduleIds[dto.Id] = entry.Id;
                modules.Add(entry);
            }

            var connections = ConnectTemplate(template.Connections, moduleIds);
            foreach (var entry in modules)
            {
                FixChannelSettings(entry);
            }

            Persist();

            _notifier.SubgraphChanged(subgraph);
            foreach (var entry in modules)
            {
                _notifier.ModuleChanged(ToDto(entry));
            }

            foreach (var connection in connections)
            {
                _notifier.ConnectionAdded(connection.Dto);
            }

            return subgraph;
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
            PortLevelsDto[] ports =
            [
                .. module
                    .ReadLevels()
                    .Where(port => port.Channels.Length > 0)
                    .Select(port => new PortLevelsDto(
                        port.Port,
                        [.. port.Channels.Select(c => c.Peak)],
                        [.. port.Channels.Select(c => c.Rms)]
                    )),
            ];
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
            if (!_modules.TryGetValue(id, out var entry))
            {
                return false;
            }

            var removedConnections = RemoveModuleCore(entry);
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
    /// Removes a subgraph. Its modules stay, with their positions converted back to absolute ones, or are removed too.
    /// </summary>
    public bool RemoveSubgraph(Guid id, bool removeModules = false)
    {
        lock (_lock)
        {
            if (!_subgraphs.Remove(id, out var subgraph))
            {
                return false;
            }

            var members = _modules.Values.Where(m => m.Settings.SubgraphId == id).ToArray();
            var removedConnections = new List<ConnectionEntry>();
            if (removeModules)
            {
                foreach (var entry in members)
                {
                    removedConnections.AddRange(RemoveModuleCore(entry));
                }

                var unavailableIds = _unavailableModules.Where(m => IsInSubgraph(m, id)).Select(m => m.Id).ToHashSet();
                _unavailableModules.RemoveAll(m => unavailableIds.Contains(m.Id));
                _unavailableConnections.RemoveAll(c =>
                    unavailableIds.Contains(c.SourceModuleId) || unavailableIds.Contains(c.TargetModuleId)
                );
            }
            else
            {
                foreach (var entry in members)
                {
                    entry.Settings = entry.Settings with
                    {
                        SubgraphId = null,
                        Position = Offset(entry.Settings.Position, subgraph.Position.X, subgraph.Position.Y),
                        TemplateModuleId = null,
                    };
                    ApplySettings(entry.Module, entry.Settings);
                }

                for (var i = 0; i < _unavailableModules.Count; i++)
                {
                    _unavailableModules[i] = WithoutSubgraph(_unavailableModules[i], subgraph);
                }
            }

            Persist();
            foreach (var connection in removedConnections)
            {
                _notifier.ConnectionRemoved(connection.Dto.Id);
            }

            foreach (var entry in members)
            {
                if (removeModules)
                {
                    _notifier.ModuleRemoved(entry.Id);
                }
                else
                {
                    _notifier.ModuleChanged(ToDto(entry));
                }
            }

            _notifier.SubgraphRemoved(id);
            return true;
        }
    }

    /// <summary>
    /// Removes a template. The subgraphs created from it stay, without the reference.
    /// </summary>
    public bool RemoveTemplate(Guid id)
    {
        lock (_lock)
        {
            if (!_templates.TryGetValue(id, out var template))
            {
                return false;
            }

            ThrowIfBuiltIn(template);
            _templates.Remove(id);

            var detached = _subgraphs
                .Values.Where(s => s.TemplateId == id)
                .Select(s => s with { TemplateId = null, TemplateRevision = null })
                .ToArray();
            foreach (var subgraph in detached)
            {
                _subgraphs[subgraph.Id] = subgraph;
            }

            Persist();
            foreach (var subgraph in detached)
            {
                _notifier.SubgraphChanged(subgraph);
            }

            _notifier.TemplatesChanged([.. _templates.Values.Select(ToDto)]);
            return true;
        }
    }

    public SubgraphTemplateDto RenameTemplate(Guid id, RenameSubgraphTemplateRequest request)
    {
        ThrowIfInvalid(StateValidator.Validate(request));

        lock (_lock)
        {
            if (!_templates.TryGetValue(id, out var template))
            {
                throw EngineRequestException.NotFound($"Subgraph template {id} not found.");
            }

            ThrowIfBuiltIn(template);
            template = template with { Name = GetTemplateName(request.Name, id) };
            _templates[id] = template;
            Persist();
            _notifier.TemplatesChanged([.. _templates.Values.Select(ToDto)]);
            return ToDto(template);
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

    /// <summary>
    /// Saves a subgraph as a new template or over an existing one, which increases its revision. The subgraph refers to the template
    /// afterwards, and its modules get template-local ids.
    /// </summary>
    public SubgraphTemplateDto SaveTemplate(SaveSubgraphTemplateRequest request)
    {
        ThrowIfInvalid(StateValidator.Validate(request));

        lock (_lock)
        {
            if (!_subgraphs.TryGetValue(request.SubgraphId, out var subgraph))
            {
                throw EngineRequestException.Invalid($"Subgraph {request.SubgraphId} not found.");
            }

            SubgraphTemplate? existing = null;
            if (request.TemplateId is { } existingId && !_templates.TryGetValue(existingId, out existing))
            {
                throw EngineRequestException.Invalid($"Subgraph template {existingId} not found.");
            }

            if (existing != null)
            {
                ThrowIfBuiltIn(existing);
            }

            var name = GetTemplateName(request.Name, existing?.Id);

            // template-local ids are kept, so subgraphs created from an earlier revision still match their modules
            var members = _modules.Values.Where(m => m.Settings.SubgraphId == subgraph.Id).ToArray();
            var usedIds = new HashSet<Guid>();
            var renumbered = new List<ModuleEntry>();
            foreach (var entry in members)
            {
                if (entry.Settings.TemplateModuleId is not { } localId || !usedIds.Add(localId))
                {
                    localId = Guid.NewGuid();
                    usedIds.Add(localId);
                    entry.Settings = entry.Settings with { TemplateModuleId = localId };
                    renumbered.Add(entry);
                }
            }

            var localIds = members.ToDictionary(m => m.Id, m => m.Settings.TemplateModuleId!.Value);
            var template = new SubgraphTemplate(
                existing?.Id ?? Guid.NewGuid(),
                name,
                (existing?.Revision ?? 0) + 1,
                subgraph.Color,
                subgraph.Size,
                [
                    .. members.Select(m =>
                        ToDto(m) with
                        {
                            Id = localIds[m.Id],
                            SubgraphId = null,
                            TemplateModuleId = null,
                        }
                    ),
                ],
                [],
                [
                    .. _connections
                        .Values.Select(c => c.Dto)
                        .Where(c => localIds.ContainsKey(c.SourceModuleId) && localIds.ContainsKey(c.TargetModuleId))
                        .Select(c => new TemplateConnectionDto(
                            localIds[c.SourceModuleId],
                            c.SourcePort,
                            localIds[c.TargetModuleId],
                            c.TargetPort,
                            c.SourceChannel,
                            c.TargetChannel
                        )),
                ]
            );
            _templates[template.Id] = template;
            subgraph = subgraph with { TemplateId = template.Id, TemplateRevision = template.Revision };
            _subgraphs[subgraph.Id] = subgraph;
            Persist();

            foreach (var entry in renumbered)
            {
                _notifier.ModuleChanged(ToDto(entry));
            }

            _notifier.SubgraphChanged(subgraph);
            _notifier.TemplatesChanged([.. _templates.Values.Select(ToDto)]);
            return ToDto(template);
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

            ThrowIfUnknownSubgraph(dto.SubgraphId);
            ThrowIfChannelCountTooLow(entry, dto);

            // channel connections keep the channels shown, also against an update made from an older copy of the module
            entry.Settings = WithSystemVolume(
                ModuleSettings.From(dto) with
                {
                    IsBypassed = dto.IsBypassed && entry.Module is EffectModule,
                    TemplateModuleId =
                        dto.SubgraphId == entry.Settings.SubgraphId ? entry.Settings.TemplateModuleId : null,
                    ShowChannels = dto.ShowChannels || GetChannelConnections(entry.Id).Any(),
                    ChannelCount = SupportsChannelCount(entry.Module) ? dto.ChannelCount : null,
                }
            );
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

    /// <summary>
    /// Replaces a subgraph's settings. Its modules are kept; a change of mute or bypass applies to them.
    /// </summary>
    public SubgraphDto UpdateSubgraph(Guid id, SubgraphDto dto)
    {
        ThrowIfInvalid(StateValidator.Validate(dto));

        lock (_lock)
        {
            if (!_subgraphs.TryGetValue(id, out var current))
            {
                throw EngineRequestException.NotFound($"Subgraph {id} not found.");
            }

            // only the engine sets the template reference; clearing it detaches the subgraph
            var subgraph =
                dto.TemplateId == null
                    ? dto with
                    {
                        Id = id,
                        TemplateRevision = null,
                    }
                    : dto with
                    {
                        Id = id,
                        TemplateId = current.TemplateId,
                        TemplateRevision = current.TemplateRevision,
                    };
            _subgraphs[id] = subgraph;
            foreach (var entry in _modules.Values.Where(m => m.Settings.SubgraphId == id))
            {
                ApplySettings(entry.Module, entry.Settings);
            }

            Persist();
            _notifier.SubgraphChanged(subgraph);
            return subgraph;
        }
    }

    /// <summary>
    /// Makes a subgraph match its template's current revision. Modules created from the template keep their ids and outside connections and
    /// take the template's settings and state, the others are removed, missing ones are added, and the connections between the modules
    /// become the template's. The subgraph keeps its name, position, collapse, mute and bypass, and takes the template's color and size.
    /// </summary>
    public SubgraphDto UpdateSubgraphFromTemplate(Guid id)
    {
        lock (_lock)
        {
            if (!_subgraphs.TryGetValue(id, out var subgraph))
            {
                throw EngineRequestException.NotFound($"Subgraph {id} not found.");
            }

            if (subgraph.TemplateId is not { } templateId)
            {
                throw EngineRequestException.Invalid($"Subgraph {id} has no template.");
            }

            var template = GetUsableTemplate(templateId);
            var members = _modules.Values.Where(m => m.Settings.SubgraphId == id).ToList();
            var moduleIds = new Dictionary<Guid, Guid>();
            var changed = new List<ModuleEntry>();
            foreach (var dto in template.Modules)
            {
                var definition = _catalog.GetDefinition(dto);
                var settings = ModuleSettings.From(dto) with { SubgraphId = id, TemplateModuleId = dto.Id };
                var entry = members.FirstOrDefault(m =>
                    m.Settings.TemplateModuleId == dto.Id && m.Definition == definition
                );
                if (entry == null)
                {
                    entry = AddModuleCore(Guid.NewGuid(), definition, settings, dto.StateObject);
                }
                else
                {
                    members.Remove(entry);
                    entry.Settings = WithSystemVolume(
                        settings with
                        {
                            IsBypassed = settings.IsBypassed && entry.Module is EffectModule,
                        }
                    );
                    ApplySettings(entry.Module, entry.Settings);
                    definition.SetState(entry.Module, dto.StateObject);
                }

                moduleIds[dto.Id] = entry.Id;
                changed.Add(entry);
            }

            var removedConnections = new List<ConnectionEntry>();
            foreach (var entry in members)
            {
                removedConnections.AddRange(RemoveModuleCore(entry));
            }

            // connections between the modules that the template has stay; the others are replaced
            var memberIds = moduleIds.Values.ToHashSet();
            var wanted = template
                .Connections.Select(c =>
                    c with
                    {
                        SourceModuleId = moduleIds[c.SourceModuleId],
                        TargetModuleId = moduleIds[c.TargetModuleId],
                    }
                )
                .ToHashSet();
            foreach (
                var connection in _connections
                    .Values.Where(c =>
                        memberIds.Contains(c.Dto.SourceModuleId) && memberIds.Contains(c.Dto.TargetModuleId)
                    )
                    .ToArray()
            )
            {
                var dto = connection.Dto;
                if (
                    !wanted.Remove(
                        new TemplateConnectionDto(
                            dto.SourceModuleId,
                            dto.SourcePort,
                            dto.TargetModuleId,
                            dto.TargetPort,
                            dto.SourceChannel,
                            dto.TargetChannel
                        )
                    )
                )
                {
                    _connections.Remove(dto.Id);
                    Disconnect(connection);
                    removedConnections.Add(connection);
                }
            }

            var addedConnections = ConnectTemplate([.. wanted], memberIds.ToDictionary(m => m));
            foreach (var entry in changed)
            {
                FixChannelSettings(entry);
            }

            subgraph = subgraph with
            {
                Color = template.Color,
                Size = template.Size,
                TemplateRevision = template.Revision,
            };
            _subgraphs[id] = subgraph;
            Persist();

            foreach (var connection in removedConnections)
            {
                _notifier.ConnectionRemoved(connection.Dto.Id);
            }

            foreach (var entry in members)
            {
                _notifier.ModuleRemoved(entry.Id);
            }

            _notifier.SubgraphChanged(subgraph);
            foreach (var entry in changed)
            {
                _notifier.ModuleChanged(ToDto(entry));
            }

            foreach (var connection in addedConnections)
            {
                _notifier.ConnectionAdded(connection.Dto);
            }

            return subgraph;
        }
    }

    /// <summary>
    /// The audio module of a module, for tests.
    /// </summary>
    internal AudioModule? GetAudioModule(Guid id)
    {
        lock (_lock)
        {
            return _modules.TryGetValue(id, out var entry) ? entry.Module : null;
        }
    }

    /// <summary>
    /// Whether the module's input takes its layout from its sources or from <see cref="AudioModule.ChannelCount"/>.
    /// </summary>
    private static bool IsFlexibleInput(ModuleEntry entry, string port)
    {
        return entry.Module.Inputs.FirstOrDefault(p => p.Name == port) is { Layout: null };
    }

    private static bool IsInSubgraph(UnavailableModule module, Guid subgraphId)
    {
        return module.Element.TryGetProperty("subgraphId", out var id)
            && id.TryGetGuid(out var guid)
            && guid == subgraphId;
    }

    private static ModulePosition? Offset(ModulePosition? position, double x, double y)
    {
        return position == null ? null : new ModulePosition(position.X + x, position.Y + y);
    }

    private static bool SupportsChannelCount(AudioModule module)
    {
        return module.Inputs.Any(p => p.Layout == null);
    }

    private static void ThrowIfBuiltIn(SubgraphTemplate template)
    {
        if (template.IsBuiltIn)
        {
            throw EngineRequestException.Invalid(
                $"The subgraph template '{template.Name}' is built in and can't be changed."
            );
        }
    }

    private static void ThrowIfInvalid(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw EngineRequestException.Invalid(errors);
        }
    }

    private static SubgraphTemplateDto ToDto(SubgraphTemplate template)
    {
        return new SubgraphTemplateDto(
            template.Id,
            template.Name,
            template.Revision,
            template.Color,
            template.Size,
            template.Modules,
            template.Connections,
            [.. template.UnavailableModules.Select(m => m.Type)],
            template.IsBuiltIn
        );
    }

    private static ModulePortLayoutsDto ToPortLayoutsDto(Guid id, AudioModule module)
    {
        return new ModulePortLayoutsDto(
            id,
            module.Inputs.ToDictionary(p => p.Name, p => PortLayoutDto.From(p.LastLayout)),
            module.Outputs.ToDictionary(p => p.Name, p => PortLayoutDto.From(p.LastLayout))
        );
    }

    /// <summary>
    /// Takes an unavailable module out of the subgraph, converting its position back to an absolute one.
    /// </summary>
    private static UnavailableModule WithoutSubgraph(UnavailableModule module, SubgraphDto subgraph)
    {
        if (!IsInSubgraph(module, subgraph.Id))
        {
            return module;
        }

        var node = JsonNode.Parse(module.Element.GetRawText())!.AsObject();
        node.Remove("subgraphId");
        if (node["position"] is JsonObject position)
        {
            position["x"] = (double?)position["x"] + subgraph.Position.X;
            position["y"] = (double?)position["y"] + subgraph.Position.Y;
        }

        return module with
        {
            Element = JsonSerializer.SerializeToElement(node),
        };
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
        var entry = new ModuleEntry(
            id,
            definition,
            module,
            settings with
            {
                IsBypassed = settings.IsBypassed && module is EffectModule,
                ChannelCount = SupportsChannelCount(module) ? settings.ChannelCount : null,
            }
        );
        _modules.Add(id, entry);
        return entry;
    }

    /// <summary>
    /// Sets the module's channel count, volume, mute and bypass, combining mute and bypass with its subgraph's.
    /// </summary>
    private void ApplySettings(AudioModule module, ModuleSettings settings)
    {
        var subgraph = settings.SubgraphId is { } id && _subgraphs.TryGetValue(id, out var s) ? s : null;
        module.ChannelCount = SupportsChannelCount(module) ? settings.ChannelCount : null;
        module.Volume = settings.Volume;
        module.IsMuted = settings.IsMuted || subgraph?.IsMuted == true;
        if (module is EffectModule effect)
        {
            effect.IsBypassed = settings.IsBypassed || subgraph?.IsBypassed == true;
        }
    }

    /// <summary>
    /// Creates the graph with the current settings and adds the modules and connections, skipping invalid ones.
    /// </summary>
    private void Build(IEnumerable<ModuleDto> modules, IEnumerable<ConnectionDto> connections)
    {
        _graph = new AudioGraph(
            new ProcessingFormat(_settings.SampleRate, _settings.FrameCount),
            _loggerFactory.CreateLogger<AudioGraph>()
        );
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

        foreach (var entry in _modules.Values)
        {
            FixChannelSettings(entry);
        }
    }

    private ConnectionEntry ConnectCore(ConnectionDto dto)
    {
        if (
            !_modules.TryGetValue(dto.SourceModuleId, out var source)
            || !_modules.TryGetValue(dto.TargetModuleId, out var target)
        )
        {
            throw EngineRequestException.Invalid("Source or target module not found.");
        }

        var sourcePort =
            source.Module.Outputs.FirstOrDefault(p => p.Name == dto.SourcePort)
            ?? throw EngineRequestException.Invalid(
                $"Module type '{source.Definition.Type}' has no output '{dto.SourcePort}'."
            );
        var targetPort =
            target.Module.Inputs.FirstOrDefault(p => p.Name == dto.TargetPort)
            ?? throw EngineRequestException.Invalid(
                $"Module type '{target.Definition.Type}' has no input '{dto.TargetPort}'."
            );

        if (
            dto.SourceChannel is < 0 or >= AudioModule.MaxChannelCount
            || dto.TargetChannel is < 0 or >= AudioModule.MaxChannelCount
        )
        {
            throw EngineRequestException.Invalid($"Channels are 0..{AudioModule.MaxChannelCount - 1}.");
        }

        if (_connections.ContainsKey(dto.Id))
        {
            throw EngineRequestException.Conflict($"Connection {dto.Id} already exists.");
        }

        try
        {
            Graph.Connect(sourcePort, targetPort, dto.SourceChannel, dto.TargetChannel);
        }
        catch (InvalidOperationException ex)
        {
            throw EngineRequestException.Conflict(ex.Message);
        }

        var entry = new ConnectionEntry(dto, sourcePort, targetPort);
        _connections.Add(dto.Id, entry);
        return entry;
    }

    /// <summary>
    /// Creates template connections between the modules the ids map to. Connections the graph rejects (e.g. a cycle through modules outside
    /// the subgraph) are skipped.
    /// </summary>
    private List<ConnectionEntry> ConnectTemplate(
        IReadOnlyList<TemplateConnectionDto> connections,
        Dictionary<Guid, Guid> moduleIds
    )
    {
        var created = new List<ConnectionEntry>();
        foreach (var connection in connections)
        {
            try
            {
                created.Add(
                    ConnectCore(
                        new ConnectionDto(
                            Guid.NewGuid(),
                            moduleIds[connection.SourceModuleId],
                            connection.SourcePort,
                            moduleIds[connection.TargetModuleId],
                            connection.TargetPort,
                            connection.SourceChannel,
                            connection.TargetChannel
                        )
                    )
                );
            }
            catch (EngineRequestException ex)
            {
                _logger.LogWarning("Skipping a template connection: {Reason}", ex.Message);
            }
        }

        return created;
    }

    private EngineStatusDto CreateStatus()
    {
        var statistics = Engine.Statistics;
        return new EngineStatusDto(
            Engine.IsRunning,
            _settings,
            statistics.Blocks,
            statistics.LateBlocks,
            statistics.MaxProcessingTime.TotalMilliseconds
        );
    }

    private void Disconnect(ConnectionEntry connection)
    {
        Graph.Disconnect(
            connection.Source,
            connection.Target,
            connection.Dto.SourceChannel,
            connection.Dto.TargetChannel
        );
    }

    /// <summary>
    /// Turns <see cref="ModuleSettings.ShowChannels"/> on for a module with connections to or from single channels, and raises its channel
    /// count to what they need.
    /// </summary>
    /// <returns>Whether the settings changed.</returns>
    private bool FixChannelSettings(ModuleEntry entry)
    {
        var settings = entry.Settings;
        if (!settings.ShowChannels && GetChannelConnections(entry.Id).Any())
        {
            settings = settings with { ShowChannels = true };
        }

        if (
            settings.ChannelCount is { } channelCount
            && GetRequiredChannelCount(entry) is var required
            && channelCount < required
        )
        {
            settings = settings with { ChannelCount = required };
        }

        if (settings == entry.Settings)
        {
            return false;
        }

        entry.Settings = settings;
        ApplySettings(entry.Module, settings);
        return true;
    }

    /// <summary>
    /// The module's position in the graph, not relative to its subgraph.
    /// </summary>
    private ModulePosition? GetAbsolutePosition(ModuleSettings settings)
    {
        return settings.SubgraphId is { } id && _subgraphs.TryGetValue(id, out var subgraph)
            ? Offset(settings.Position, subgraph.Position.X, subgraph.Position.Y)
            : settings.Position;
    }

    /// <summary>
    /// The connections that take a single channel of the module's outputs or add to a single channel of its inputs. Those of unavailable
    /// modules don't count, as the API and UI don't show them; restoring them with their plugin fixes the settings (<see cref="Build"/>).
    /// </summary>
    private IEnumerable<ConnectionDto> GetChannelConnections(Guid moduleId)
    {
        return _connections
            .Values.Select(c => c.Dto)
            .Where(c =>
                (c.SourceModuleId == moduleId && c.SourceChannel != null)
                || (c.TargetModuleId == moduleId && c.TargetChannel != null)
            );
    }

    /// <summary>
    /// The lowest channel count the module's connections to single channels of its flexible inputs need; 1 without any.
    /// </summary>
    private int GetRequiredChannelCount(ModuleEntry entry)
    {
        return GetChannelConnections(entry.Id)
            .Where(c => c.TargetModuleId == entry.Id && c.TargetChannel != null && IsFlexibleInput(entry, c.TargetPort))
            .Select(c => c.TargetChannel!.Value + 1)
            .DefaultIfEmpty(1)
            .Max();
    }

    /// <summary>
    /// The trimmed name, which no other template may have (ignoring case).
    /// </summary>
    private string GetTemplateName(string name, Guid? templateId)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            throw EngineRequestException.Invalid("The template name is empty.");
        }

        if (
            _templates.Values.Any(t =>
                t.Id != templateId && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            throw EngineRequestException.Conflict($"A subgraph template named '{name}' exists.");
        }

        return name;
    }

    /// <summary>
    /// A template whose modules can all be created.
    /// </summary>
    private SubgraphTemplate GetUsableTemplate(Guid id)
    {
        if (!_templates.TryGetValue(id, out var template))
        {
            throw EngineRequestException.NotFound($"Subgraph template {id} not found.");
        }

        if (template.UnavailableModules.Count > 0)
        {
            var types = string.Join(", ", template.UnavailableModules.Select(m => m.Type).Distinct());
            throw EngineRequestException.Invalid(
                $"The template '{template.Name}' has modules whose plugin isn't loaded: {types}."
            );
        }

        foreach (var module in template.Modules)
        {
            ThrowIfInvalid(StateValidator.Validate(module, "modules"));
        }

        return template;
    }

    /// <summary>
    /// Adds the loaded plugins' templates as built-in ones. An invalid one, or one whose id or name another plugin's template has, is skipped.
    /// </summary>
    private void LoadPluginTemplates()
    {
        foreach (var plugin in _plugins.Plugins.Where(p => p.IsLoaded && p.Manifest!.Templates != null))
        {
            List<SubgraphTemplate> templates;
            try
            {
                templates = _store.ReadPluginTemplates(Path.Combine(plugin.Directory, plugin.Manifest!.Templates!));
            }
            catch (Exception ex) when (ex is JsonException or IOException or NotSupportedException)
            {
                _logger.LogWarning(ex, "The templates of the plugin {Id} can't be read.", plugin.Id);
                continue;
            }

            foreach (var template in templates)
            {
                var moduleIds = template
                    .Modules.Select(m => m.Id)
                    .Concat(template.UnavailableModules.Select(m => m.Id))
                    .ToHashSet();
                var errors = template.Modules.SelectMany(m => StateValidator.Validate(m).Keys).ToArray();
                if (
                    string.IsNullOrWhiteSpace(template.Name)
                    || errors.Length > 0
                    || template.Connections.Any(c =>
                        !moduleIds.Contains(c.SourceModuleId) || !moduleIds.Contains(c.TargetModuleId)
                    )
                    || _templates.Values.Any(t =>
                        string.Equals(t.Name, template.Name, StringComparison.OrdinalIgnoreCase)
                    )
                    || !_templates.TryAdd(template.Id, template with { IsBuiltIn = true })
                )
                {
                    _logger.LogWarning(
                        "Skipping the template {Id} of the plugin {Plugin}: it's invalid or its id or name is taken.",
                        template.Id,
                        plugin.Id
                    );
                }
            }
        }
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
        _store.Save(
            new EngineConfiguration
            {
                Settings = _settings,
                Preferences = _preferences,
                Modules = [.. _modules.Values.Select(ToDto)],
                Subgraphs = [.. _subgraphs.Values],
                Templates = [.. _templates.Values.Where(t => !t.IsBuiltIn)],
                UnavailableModules = [.. _unavailableModules],
                Connections = [.. _connections.Values.Select(c => c.Dto), .. _unavailableConnections],
            }
        );
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

    /// <summary>
    /// Removes a module and its connections from the graph, and returns the connections.
    /// </summary>
    private ConnectionEntry[] RemoveModuleCore(ModuleEntry entry)
    {
        _modules.Remove(entry.Id);
        var connections = _connections
            .Values.Where(c => c.Dto.SourceModuleId == entry.Id || c.Dto.TargetModuleId == entry.Id)
            .ToArray();
        foreach (var connection in connections)
        {
            _connections.Remove(connection.Dto.Id);
        }

        _unavailableConnections.RemoveAll(c => c.SourceModuleId == entry.Id || c.TargetModuleId == entry.Id);
        Graph.Remove(entry.Module);
        entry.Module.StateChanged -= OnModuleStateChanged;
        entry.Module.Dispose();
        return connections;
    }

    /// <summary>
    /// Renames the user's templates that have a built-in template's name, e.g. one that a plugin update added, to "name (custom)".
    /// </summary>
    /// <returns>Whether a template was renamed.</returns>
    private bool RenameTemplatesNamedLikeBuiltIns()
    {
        var builtInNames = _templates
            .Values.Where(t => t.IsBuiltIn)
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var renamed = false;
        foreach (var template in _templates.Values.Where(t => !t.IsBuiltIn && builtInNames.Contains(t.Name)).ToArray())
        {
            var name = $"{template.Name} (custom)";
            for (
                var i = 2;
                _templates.Values.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
                i++
            )
            {
                name = $"{template.Name} (custom {i})";
            }

            _logger.LogInformation(
                "Renaming the template {Name} to {NewName}: a built-in template has its name.",
                template.Name,
                name
            );
            _templates[template.Id] = template with { Name = name };
            renamed = true;
        }

        return renamed;
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

    /// <summary>
    /// Rejects an update that sets a channel count lower than the module's connections to single channels need.
    /// </summary>
    private void ThrowIfChannelCountTooLow(ModuleEntry entry, ModuleDto dto)
    {
        if (
            SupportsChannelCount(entry.Module)
            && dto.ChannelCount is { } channelCount
            && GetRequiredChannelCount(entry) is var required
            && channelCount < required
        )
        {
            throw EngineRequestException.Invalid(
                new Dictionary<string, string[]>
                {
                    ["channelCount"] = [$"The module's connections to single channels need {required} channels."],
                }
            );
        }
    }

    private void ThrowIfUnknownSubgraph(Guid? id)
    {
        if (id is { } subgraphId && !_subgraphs.ContainsKey(subgraphId))
        {
            throw EngineRequestException.Invalid($"Subgraph {subgraphId} not found.");
        }
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
            ? settings with
            {
                Volume = level.Gain,
                IsMuted = level.IsMuted,
            }
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
