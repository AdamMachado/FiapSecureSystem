namespace Fiap.SecureSystem.ApiGateway.Contracts.Requests;

public sealed record LoginRequest(
    string Email,
    string Password);
