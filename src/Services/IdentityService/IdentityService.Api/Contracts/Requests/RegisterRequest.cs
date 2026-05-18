namespace IdentityService.Api.Contracts.Requests;

public sealed record RegisterRequest(
    string Email,
    string DisplayName,
    string Password);
