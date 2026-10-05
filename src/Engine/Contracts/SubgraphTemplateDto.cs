using System.ComponentModel.DataAnnotations;

namespace Micser.Engine.Contracts;

/// <summary>
/// A connection between two modules of a subgraph template, by their template-local ids.
/// </summary>
public sealed record TemplateConnectionDto(Guid SourceModuleId, string SourcePort, Guid TargetModuleId, string TargetPort);

/// <summary>
/// A saved subgraph: its frame, modules and the connections between them, to create subgraphs from.
/// </summary>
/// <param name="Revision">Increases each time the template is saved over; subgraphs with a lower one are outdated.</param>
/// <param name="Modules">The modules with template-local ids and positions relative to the subgraph.</param>
/// <param name="UnavailableTypes">The types of modules in the template whose plugin isn't loaded. A template with any can't be used.</param>
/// <param name="IsBuiltIn">Provided by a plugin and updated with it; it can't be renamed, saved over or removed.</param>
public sealed record SubgraphTemplateDto(
    Guid Id,
    string Name,
    int Revision,
    SubgraphColor Color,
    SubgraphSize Size,
    IReadOnlyList<ModuleDto> Modules,
    IReadOnlyList<TemplateConnectionDto> Connections,
    IReadOnlyList<string> UnavailableTypes,
    bool IsBuiltIn);

/// <param name="SubgraphId">The subgraph to save; it refers to the template afterwards.</param>
/// <param name="Name">Template names are unique, ignoring case.</param>
/// <param name="TemplateId">The template to save over, keeping its id; a new template if null.</param>
public sealed record SaveSubgraphTemplateRequest(Guid SubgraphId, [Required, MaxLength(100)] string Name, Guid? TemplateId = null);

/// <param name="Position">The new subgraph's position.</param>
public sealed record InstantiateSubgraphTemplateRequest([Required] ModulePosition Position);

public sealed record RenameSubgraphTemplateRequest([Required, MaxLength(100)] string Name);
