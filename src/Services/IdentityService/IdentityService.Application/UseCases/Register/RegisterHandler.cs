using IdentityService.Application.Abstractions.Authentication;
using IdentityService.Domain.Entities;
using IdentityService.Domain.ValueObjects;
using Shared.Kernel.Result;

namespace IdentityService.Application.UseCases.Register;

public sealed class RegisterHandler
{
    private static readonly IReadOnlyCollection<string> DefaultRoles = ["Analyst"];
    private static readonly IReadOnlyCollection<string> DefaultScopes = ["analysis.read", "analysis.write", "report.read"];

    private readonly IUserCredentialStore _userCredentialStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;

    public RegisterHandler(
        IUserCredentialStore userCredentialStore,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer)
    {
        _userCredentialStore = userCredentialStore;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<Result<RegisterResult>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
            return Result.Failure<RegisterResult>(Error.Validation("identity.email_required", "Email is required."));

        if (string.IsNullOrWhiteSpace(command.DisplayName))
            return Result.Failure<RegisterResult>(Error.Validation("identity.display_name_required", "Display name is required."));

        if (string.IsNullOrWhiteSpace(command.Password))
            return Result.Failure<RegisterResult>(Error.Validation("identity.password_required", "Password is required."));

        if (command.Password.Length < 8)
            return Result.Failure<RegisterResult>(Error.Validation("identity.password_too_short", "Password must contain at least 8 characters."));

        EmailAddress email;

        try
        {
            email = EmailAddress.Create(command.Email);
        }
        catch (ArgumentException)
        {
            return Result.Failure<RegisterResult>(Error.Validation("identity.email_invalid", "Email is invalid."));
        }

        if (await _userCredentialStore.ExistsByEmailAsync(email.Value, cancellationToken))
            return Result.Failure<RegisterResult>(Error.Conflict("identity.email_already_exists", "This email is already registered."));

        var user = User.Create(Guid.NewGuid(), email, command.DisplayName, DefaultRoles, DefaultScopes, true);
        var passwordHash = _passwordHasher.Hash(command.Password);

        await _userCredentialStore.CreateAsync(
            user.Id,
            user.Email.Value,
            user.DisplayName,
            passwordHash,
            user.Roles,
            user.Scopes,
            cancellationToken);

        var token = _tokenIssuer.Issue(user);

        return Result.Success(new RegisterResult(
            user.Id,
            user.DisplayName,
            user.Email.Value,
            token.Token,
            "Bearer",
            token.ExpiresInSeconds,
            user.Roles,
            user.Scopes));
    }
}
