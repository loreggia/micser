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

            var modules = new List<ModuleDto>();
            var unavailableModules = new List<UnavailableModule>();
            foreach (var element in file.Modules)
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

            return new EngineConfiguration
            {
                Settings = file.Settings ?? new EngineSettingsDto(),
                Preferences = file.Preferences ?? new UiPreferencesDto(),
                Modules = modules,
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
                Modules = [.. configuration.Modules.Select(m => JsonSerializer.SerializeToElement(m, _json)), .. configuration.UnavailableModules.Select(m => m.Element)],
                Preferences = configuration.Preferences,
                Settings = configuration.Settings,
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

        public int Version { get; init; }
    }
}
