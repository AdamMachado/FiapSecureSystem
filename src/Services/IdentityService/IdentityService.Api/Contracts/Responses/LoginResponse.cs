namespace IdentityService.Api.Contracts.Responses;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    LoginUserResponse User);
