namespace IdentityService.Application.Abstractions.Authentication;

public sealed record AccessToken(
    string Token,
    DateTime ExpiresAtUtc,
    long ExpiresInSeconds);
