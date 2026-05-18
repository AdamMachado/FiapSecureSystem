namespace Fiap.SecureSystem.WebApp.Clients.Identity.Contracts;

public sealed record LoginRequest(
    string Email,
    string Password);
