using Shared.Contracts.IntegrationEvents.Enums;

namespace Shared.Contracts.IntegrationEvents.Schemas;

public sealed record IdentifiedComponentDto(
    string Id,
    string Name,
    ComponentType Type,
    string? Description,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> ConnectedTo,
    IReadOnlyDictionary<string, string>? Metadata);
