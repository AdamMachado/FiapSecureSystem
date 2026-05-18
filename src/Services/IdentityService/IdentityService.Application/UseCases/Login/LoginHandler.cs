using IdentityService.Application.Abstractions.Authentication;
using Shared.Kernel.Result;

namespace IdentityService.Application.UseCases.Login;

public sealed class LoginHandler
{
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("identity.invalid_credentials", "Invalid email or password.");

    private static readonly Error InactiveUser =
        Error.Forbidden("identity.inactive_user", "The user is inactive.");

    private readonly IUserCredentialStore _userCredentialStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;

    public LoginHandler(
        IUserCredentialStore userCredentialStore,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer)
    {
        _userCredentialStore = userCredentialStore;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<Result<LoginResult>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
            return Result.Failure<LoginResult>(Error.Validation("identity.email_required", "Email is required."));

        if (string.IsNullOrWhiteSpace(command.Password))
            return Result.Failure<LoginResult>(Error.Validation("identity.password_required", "Password is required."));

        var credential = await _userCredentialStore.FindByEmailAsync(command.Email, cancellationToken);

        if (credential is null)
            return Result.Failure<LoginResult>(InvalidCredentials);

        if (!credential.User.IsActive)
            return Result.Failure<LoginResult>(InactiveUser);

        if (!_passwordHasher.Verify(command.Password, credential.PasswordHash))
            return Result.Failure<LoginResult>(InvalidCredentials);

        var token = _tokenIssuer.Issue(credential.User);

        return Result.Success(new LoginResult(
            credential.User.Id,
            credential.User.DisplayName,
            credential.User.Email.Value,
            token.Token,
            "Bearer",
            token.ExpiresInSeconds,
            credential.User.Roles,
            credential.User.Scopes));
    }
}
