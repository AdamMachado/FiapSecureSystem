namespace Fiap.SecureSystem.ApiGateway.Contracts.Responses;

public sealed record LoginUserResponse(
    Guid Id,
    string Name,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes);
