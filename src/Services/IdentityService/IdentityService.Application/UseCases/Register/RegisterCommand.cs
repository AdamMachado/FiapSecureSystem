namespace IdentityService.Application.UseCases.Register;

public sealed record RegisterCommand(
    string Email,
    string DisplayName,
    string Password);
