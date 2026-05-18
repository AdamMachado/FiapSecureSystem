namespace IdentityService.Application.UseCases.Login;

public sealed record LoginCommand(
    string Email,
    string Password);
