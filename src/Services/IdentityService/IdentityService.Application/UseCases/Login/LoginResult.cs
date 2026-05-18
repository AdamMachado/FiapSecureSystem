namespace IdentityService.Application.UseCases.Login;

public sealed record LoginResult(
    Guid UserId,
    string DisplayName,
    string Email,
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Scopes);
