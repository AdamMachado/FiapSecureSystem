using IdentityService.Api.Configuration;
using IdentityService.Api.Contracts.Requests;
using IdentityService.Api.Contracts.Responses;
using IdentityService.Application.UseCases.Login;
using IdentityService.Application.UseCases.Register;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] LoginHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        if (result.IsFailure)
            return result.ToProblemHttpResult();

        var response = new LoginResponse(
            result.Value.AccessToken,
            result.Value.TokenType,
            result.Value.ExpiresIn,
            new LoginUserResponse(
                result.Value.UserId,
                result.Value.DisplayName,
                result.Value.Email,
                result.Value.Roles,
                result.Value.Scopes));

        return Results.Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        [FromServices] RegisterHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RegisterCommand(request.Email, request.DisplayName, request.Password),
            cancellationToken);

        if (result.IsFailure)
            return result.ToProblemHttpResult();

        var response = new LoginResponse(
            result.Value.AccessToken,
            result.Value.TokenType,
            result.Value.ExpiresIn,
            new LoginUserResponse(
                result.Value.UserId,
                result.Value.DisplayName,
                result.Value.Email,
                result.Value.Roles,
                result.Value.Scopes));

        return Results.Created("/api/auth/register", response);
    }
}
