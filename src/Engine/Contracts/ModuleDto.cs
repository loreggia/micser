using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Micser.Engine.Contracts;

/// <summary>
/// Position of a module in the UI's graph editor.
/// </summary>
public sealed record ModulePosition(double X, double Y);

/// <summary>
/// A module and its settings. The concrete type is <see cref="ModuleDto{TState}"/>; in JSON the <c>type</c> property
/// names the module type and selects the state schema (registered at runtime by <see cref="Modules.ModuleCatalog"/>).
/// </summary>
public abstract record ModuleDto
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Passes the audio through unprocessed. Only effect modules support it; it is ignored for all others.
    /// </summary>
    public bool IsBypassed { get; init; }

    /// <summary>
    /// Shows only the name, mute and connectors in the UI's graph editor.
    /// </summary>
    public bool IsCollapsed { get; init; }

    public bool IsMuted { get; init; }

    /// <summary>
    /// A name chosen by the user.
    /// </summary>
    [MaxLength(100)]
    public string? Name { get; init; }

    public ModulePosition? Position { get; init; }

    /// <summary>
    /// Follows the volume and mute of Windows' default output device (its volume slider and keys). The engine then sets
    /// <see cref="Volume"/> and <see cref="IsMuted"/>, and the values sent with an update are ignored.
    /// </summary>
    public bool UseSystemVolume { get; init; }

    [JsonIgnore]
    public abstract object StateObject { get; }

    /// <summary>
    /// Output volume, 0..1.
    /// </summary>
    [Range(0f, 1f)]
    public float Volume { get; init; } = 1f;
}

public sealed record ModuleDto<TState> : ModuleDto
    where TState : class
{
    public required TState State { get; init; }

    [JsonIgnore]
    public override object StateObject => State;
}

/// <param name="Type">A module type from <c>GET /api/module-types</c>.</param>
public sealed record CreateModuleRequest([Required] string Type, [MaxLength(100)] string? Name = null, ModulePosition? Position = null);

/// <param name="Type">The name identifying the module type, e.g. "Gain".</param>
/// <param name="Inputs">Names of the input ports.</param>
/// <param name="Outputs">Names of the output ports.</param>
/// <param name="DefaultState">The state of a new module of this type.</param>
/// <param name="SupportsBypass">Whether <see cref="ModuleDto.IsBypassed"/> has an effect.</param>
public sealed record ModuleTypeDto(string Type, IReadOnlyList<string> Inputs, IReadOnlyList<string> Outputs, object DefaultState, bool SupportsBypass);
