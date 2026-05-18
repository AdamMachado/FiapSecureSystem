namespace IdentityService.Api.Contracts.Responses;

public sealed record LoginUserResponse(
    Guid Id,
    string Name,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes);
