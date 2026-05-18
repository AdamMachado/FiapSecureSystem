namespace Fiap.SecureSystem.WebApp.Clients.Identity.Contracts;

public sealed record RegisterRequest(
    string Email,
    string DisplayName,
    string Password);
