namespace Fiap.SecureSystem.ApiGateway.Contracts.Requests;

public sealed record RegisterRequest(
    string Email,
    string DisplayName,
    string Password);
