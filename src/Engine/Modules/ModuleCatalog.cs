using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Micser.Audio;
using Micser.Engine.Contracts;

namespace Micser.Engine.Modules;

/// <summary>
/// The module types registered by plugins, and the JSON polymorphism that maps <see cref="ModuleDto"/>'s <c>type</c>
/// to <see cref="ModuleDto{TState}"/> of the module's state type.
/// </summary>
public sealed class ModuleCatalog
{
    private static readonly MethodInfo CreateDtoMethod = typeof(ModuleCatalog).GetMethod(nameof(CreateDtoCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly Dictionary<Type, AudioModuleDefinition> _byDtoType = [];
    private readonly Dictionary<string, AudioModuleDefinition> _byType = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, Func<Guid, ModuleSettings, object, ModuleDto>> _dtoFactories = [];

    public ModuleCatalog(IEnumerable<AudioModuleDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            var dtoType = typeof(ModuleDto<>).MakeGenericType(definition.StateType);
            if (!_byType.TryAdd(definition.Type, definition))
            {
                throw new InvalidOperationException($"The module type '{definition.Type}' is registered more than once.");
            }

            if (!_byDtoType.TryAdd(dtoType, definition))
            {
                throw new InvalidOperationException($"The module types '{_byDtoType[dtoType].Type}' and '{definition.Type}' share the state type {definition.StateType.Name}.");
            }

            _dtoFactories[definition.StateType] = CreateDtoMethod.MakeGenericMethod(definition.StateType).CreateDelegate<Func<Guid, ModuleSettings, object, ModuleDto>>();
        }
    }

    public IEnumerable<AudioModuleDefinition> Definitions => _byType.Values;

    /// <summary>
    /// Adds the registered module types as derived types of <see cref="ModuleDto"/>. Use as a
    /// <see cref="DefaultJsonTypeInfoResolver.Modifiers"/> entry.
    /// </summary>
    public void AddModuleTypes(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(ModuleDto))
        {
            return;
        }

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "type",
            UnknownDerivedTypeHandling = System.Text.Json.Serialization.JsonUnknownDerivedTypeHandling.FailSerialization,
        };

        foreach (var (dtoType, definition) in _byDtoType)
        {
            typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(dtoType, definition.Type));
        }
    }

    public ModuleDto CreateDto(Guid id, AudioModuleDefinition definition, ModuleSettings settings, object state)
    {
        return _dtoFactories[definition.StateType](id, settings, state);
    }

    /// <summary>
    /// Returns the module type of a DTO.
    /// </summary>
    public AudioModuleDefinition GetDefinition(ModuleDto dto)
    {
        return _byDtoType[dto.GetType()];
    }

    public AudioModuleDefinition? TryGetDefinition(string type)
    {
        return _byType.GetValueOrDefault(type);
    }

    private static ModuleDto CreateDtoCore<TState>(Guid id, ModuleSettings settings, object state)
        where TState : class
    {
        return new ModuleDto<TState>
        {
            Id = id,
            Name = settings.Name,
            Position = settings.Position,
            Volume = settings.Volume,
            IsMuted = settings.IsMuted,
            UseSystemVolume = settings.UseSystemVolume,
            IsBypassed = settings.IsBypassed,
            IsCollapsed = settings.IsCollapsed,
            State = (TState)state,
        };
    }
}

/// <summary>
/// The settings every module has, besides its state.
/// </summary>
public sealed record ModuleSettings(string? Name, ModulePosition? Position, float Volume, bool IsMuted, bool IsBypassed, bool UseSystemVolume, bool IsCollapsed)
{
    public static ModuleSettings From(ModuleDto dto)
    {
        return new ModuleSettings(dto.Name, dto.Position, dto.Volume, dto.IsMuted, dto.IsBypassed, dto.UseSystemVolume, dto.IsCollapsed);
    }
}
