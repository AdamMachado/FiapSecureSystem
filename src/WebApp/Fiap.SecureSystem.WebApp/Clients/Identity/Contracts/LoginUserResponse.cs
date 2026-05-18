namespace Fiap.SecureSystem.WebApp.Clients.Identity.Contracts;

public sealed record LoginUserResponse(
    Guid Id,
    string Name,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes);
