using System.Text.Json;
using Micser.Engine.Contracts;
using Micser.Engine.Modules;
using Microsoft.Extensions.Options;

namespace Micser.Engine.Configuration;

/// <summary>
/// The persisted routing graph and settings.
/// </summary>
public sealed class EngineConfiguration
{
    public const int CurrentVersion = 1;

    public List<ConnectionDto> Connections { get; init; } = [];

    public List<ModuleDto> Modules { get; init; } = [];

    public UiPreferencesDto Preferences { get; init; } = new();

    public EngineSettingsDto Settings { get; init; } = new();

    public List<SubgraphDto> Subgraphs { get; init; } = [];

    public List<SubgraphTemplate> Templates { get; init; } = [];

    /// <summary>
    /// Modules whose plugin isn't loaded. They're kept as they are, so they come back when the plugin does.
    /// </summary>
    public List<UnavailableModule> UnavailableModules { get; init; } = [];

    public int Version { get; init; } = CurrentVersion;
}

/// <summary>
/// A module in the configuration whose type no loaded plugin provides.
/// </summary>
public sealed record UnavailableModule(Guid Id, string Type, JsonElement Element);

/// <summary>
/// A subgraph template. Like the graph's, its modules of unknown types are kept as they are.
/// </summary>
/// <param name="Modules">The modules with template-local ids and positions relative to the subgraph.</param>
/// <param name="IsBuiltIn">Provided by a plugin: loaded at every start, not saved, and can't be changed.</param>
public sealed record SubgraphTemplate(
    Guid Id,
    string Name,
    int Revision,
    SubgraphColor Color,
    SubgraphSize Size,
    IReadOnlyList<ModuleDto> Modules,
    IReadOnlyList<UnavailableModule> UnavailableModules,
    IReadOnlyList<TemplateConnectionDto> Connections,
    bool IsBuiltIn = false);

/// <summary>
/// Loads and saves <see cref="EngineConfiguration"/> as JSON. Saves are debounced and written atomically.
/// </summary>
public sealed class EngineConfigStore : IDisposable
{
    private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(500);

    private readonly ModuleCatalog _catalog;
    private readonly JsonSerializerOptions _json;
    private readonly Lock _lock = new();
    private readonly ILogger _logger;
    private readonly string _path;
    private readonly Timer _timer;
    private readonly Lock _writeLock = new();
    private EngineConfiguration? _pending;

    public EngineConfigStore(IOptions<EngineOptions> options, ModuleCatalog catalog, ILogger<EngineConfigStore> logger)
    {
        _path = options.Value.ConfigPath;
        _catalog = catalog;
        _json = EngineJson.CreateOptions(catalog);
        _logger = logger;
        _timer = new Timer(_ => Flush());
    }

    public string Path => _path;

    public void Dispose()
    {
        // wait for a running timer callback, so no write happens after disposal
        using (var disposed = new ManualResetEvent(false))
        {
            if (_timer.Dispose(disposed))
            {
                disposed.WaitOne();
            }
        }

        Flush();
    }

    /// <summary>
    /// Writes a pending save right away.
    /// </summary>
    public void Flush()
    {
        lock (_writeLock)
        {
            EngineConfiguration? configuration;
            lock (_lock)
            {
                configuration = _pending;
                _pending = null;
            }

            if (configuration != null)
            {
                Write(configuration);
            }
        }
    }

    /// <summary>
    /// Reads the configuration. A missing file yields an empty configuration; an unreadable one is backed up first.
    /// Modules of unknown types are kept as <see cref="EngineConfiguration.UnavailableModules"/>; invalid entries are skipped.
    /// </summary>
    public EngineConfiguration Load()
    {
        if (!File.Exists(_path))
        {
            _logger.LogInformation("No configuration at {Path}; starting empty.", _path);
            return new EngineConfiguration();
        }

        try
        {
            using var stream = File.OpenRead(_path);
            var file = JsonSerializer.Deserialize<ConfigurationFile>(stream, _json) ?? throw new JsonException("The file is empty.");
            if (file.Version != EngineConfiguration.CurrentVersion)
            {
                throw new JsonException($"Unsupported version {file.Version}.");
            }

            var (modules, unavailableModules) = ReadModules(file.Modules);
            return new EngineConfiguration
            {
                Settings = file.Settings ?? new EngineSettingsDto(),
                Preferences = file.Preferences ?? new UiPreferencesDto(),
                Modules = modules,
                Subgraphs = file.Subgraphs ?? [],
                Templates = ReadTemplates(file.Templates ?? []),
                UnavailableModules = unavailableModules,
                Connections = file.Connections ?? [],
            };
        }
        catch (Exception ex) when (ex is JsonException or IOException or NotSupportedException)
        {
            var backup = $"{_path}.{DateTime.Now:yyyyMMdd-HHmmss}.bak";
            _logger.LogError(ex, "The configuration at {Path} can't be read; moving it to {Backup} and starting empty.", _path, backup);
            File.Move(_path, backup, overwrite: true);
            return new EngineConfiguration();
        }
    }

    /// <summary>
    /// Reads a plugin's templates file: a JSON array of templates in the configuration's format.
    /// </summary>
    /// <exception cref="JsonException">The file can't be read.</exception>
    /// <exception cref="IOException">The file can't be read.</exception>
    public List<SubgraphTemplate> ReadPluginTemplates(string path)
    {
        using var stream = File.OpenRead(path);
        return ReadTemplates(JsonSerializer.Deserialize<List<TemplateFile>>(stream, _json) ?? throw new JsonException("The file is empty."));
    }

    /// <summary>
    /// Schedules a save; only the last configuration within the save delay is written.
    /// </summary>
    public void Save(EngineConfiguration configuration)
    {
        lock (_lock)
        {
            _pending = configuration;
        }

        try
        {
            _timer.Change(SaveDelay, Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException)
        {
            Flush();
        }
    }

    /// <summary>
    /// Reads modules, keeping those of unknown types as they are and skipping invalid ones.
    /// </summary>
    private (List<ModuleDto> Modules, List<UnavailableModule> UnavailableModules) ReadModules(IEnumerable<JsonElement> elements)
    {
        var modules = new List<ModuleDto>();
        var unavailableModules = new List<UnavailableModule>();
        foreach (var element in elements)
        {
            if (TryReadUnavailable(element) is { } unavailable)
            {
                _logger.LogWarning("Keeping module {Id} of the unknown type {Type}; its plugin isn't loaded.", unavailable.Id, unavailable.Type);
                unavailableModules.Add(unavailable);
                continue;
            }

            try
            {
                modules.Add(element.Deserialize<ModuleDto>(_json) ?? throw new JsonException("Empty module."));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Skipping a module in the configuration: {Module}", element.GetRawText());
            }
        }

        return (modules, unavailableModules);
    }

    private List<SubgraphTemplate> ReadTemplates(IEnumerable<TemplateFile> templates)
    {
        var result = new List<SubgraphTemplate>();
        foreach (var template in templates)
        {
            var (modules, unavailableModules) = ReadModules(template.Modules);
            result.Add(new SubgraphTemplate(template.Id, template.Name, template.Revision, template.Color, template.Size, modules, unavailableModules, template.Connections));
        }

        return result;
    }

    private List<JsonElement> SerializeModules(IEnumerable<ModuleDto> modules, IEnumerable<UnavailableModule> unavailableModules)
    {
        return [.. modules.Select(m => JsonSerializer.SerializeToElement(m, _json)), .. unavailableModules.Select(m => m.Element)];
    }

    /// <summary>
    /// Returns the module if it has an id and a type that no plugin provides.
    /// </summary>
    private UnavailableModule? TryReadUnavailable(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String
            && _catalog.TryGetDefinition(type.GetString()!) == null
            && element.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String && id.TryGetGuid(out var guid)
                ? new UnavailableModule(guid, type.GetString()!, element.Clone())
                : null;
    }

    private void Write(EngineConfiguration configuration)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
            var temporary = _path + ".tmp";
            var file = new ConfigurationFile
            {
                Connections = configuration.Connections,
                Modules = SerializeModules(configuration.Modules, configuration.UnavailableModules),
                Preferences = configuration.Preferences,
                Settings = configuration.Settings,
                Subgraphs = configuration.Subgraphs,
                Templates = [.. configuration.Templates.Select(t => new TemplateFile
                {
                    Id = t.Id,
                    Name = t.Name,
                    Revision = t.Revision,
                    Color = t.Color,
                    Size = t.Size,
                    Modules = SerializeModules(t.Modules, t.UnavailableModules),
                    Connections = [.. t.Connections],
                })],
                Version = configuration.Version,
            };

            using (var stream = File.Create(temporary))
            {
                JsonSerializer.Serialize(stream, file, _json);
            }

            File.Move(temporary, _path, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saving the configuration to {Path} failed.", _path);
        }
    }

    private sealed class ConfigurationFile
    {
        public List<ConnectionDto>? Connections { get; init; }

        public List<JsonElement> Modules { get; init; } = [];

        public UiPreferencesDto? Preferences { get; init; }

        public EngineSettingsDto? Settings { get; init; }

        public List<SubgraphDto>? Subgraphs { get; init; }

        public List<TemplateFile>? Templates { get; init; }

        public int Version { get; init; }
    }

    private sealed class TemplateFile
    {
        public SubgraphColor Color { get; init; }

        public List<TemplateConnectionDto> Connections { get; init; } = [];

        public Guid Id { get; init; }

        public List<JsonElement> Modules { get; init; } = [];

        public string Name { get; init; } = "";

        public int Revision { get; init; }

        public SubgraphSize Size { get; init; } = new(400, 300);
    }
}
