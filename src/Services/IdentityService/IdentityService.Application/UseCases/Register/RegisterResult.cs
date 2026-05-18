namespace IdentityService.Application.UseCases.Register;

public sealed record RegisterResult(
    Guid UserId,
    string DisplayName,
    string Email,
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes);
