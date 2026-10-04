using System.ComponentModel.DataAnnotations;

namespace Micser.Engine.Contracts;

/// <summary>
/// The color of a subgraph's frame; the UI maps it to its theme's palette.
/// </summary>
public enum SubgraphColor
{
    Neutral,
    Blue,
    Green,
    Yellow,
    Orange,
    Red,
    Purple,
    Teal,
}

/// <summary>
/// Size of a subgraph's frame in the UI's graph editor.
/// </summary>
public sealed record SubgraphSize([Range(1, 100000)] double Width, [Range(1, 100000)] double Height);

/// <summary>
/// A group of modules in the UI's graph editor. Its modules refer to it with <see cref="ModuleDto.SubgraphId"/>, and their
/// positions are relative to <see cref="Position"/>.
/// </summary>
/// <param name="Name">A name chosen by the user.</param>
/// <param name="IsCollapsed">Shows the subgraph as one node with the ports of the connections that cross its border.</param>
/// <param name="IsMuted">Mutes all its modules, in addition to their own mute.</param>
/// <param name="IsBypassed">Bypasses all its effect modules, in addition to their own bypass.</param>
public sealed record SubgraphDto(
    Guid Id,
    [MaxLength(100)] string? Name,
    [Required] ModulePosition Position,
    [Required] SubgraphSize Size,
    SubgraphColor Color = SubgraphColor.Blue,
    bool IsCollapsed = false,
    bool IsMuted = false,
    bool IsBypassed = false);

/// <param name="Position">The frame's position; the modules' positions are converted to be relative to it.</param>
/// <param name="ModuleIds">The modules to move into the subgraph, also from other subgraphs.</param>
public sealed record CreateSubgraphRequest(
    [MaxLength(100)] string? Name,
    [Required] ModulePosition Position,
    [Required] SubgraphSize Size,
    IReadOnlyList<Guid> ModuleIds,
    SubgraphColor Color = SubgraphColor.Blue);
