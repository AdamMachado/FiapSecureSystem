using Fiap.SecureSystem.ApiGateway.Contracts.Requests;
using Fiap.SecureSystem.ApiGateway.Contracts.Responses;
using Fiap.SecureSystem.ApiGateway.Services.Common;
using Fiap.SecureSystem.ApiGateway.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.SecureSystem.ApiGateway.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] IIdentityServiceClient identityServiceClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await identityServiceClient.LoginAsync(
                request.Email,
                request.Password,
                cancellationToken);

            return Ok(response);
        }
        catch (UpstreamServiceException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        [FromServices] IIdentityServiceClient identityServiceClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await identityServiceClient.RegisterAsync(
                request.Email,
                request.DisplayName,
                request.Password,
                cancellationToken);

            return Created("/api/auth/register", response);
        }
        catch (UpstreamServiceException exception)
        {
            return ToProblemResult(exception);
        }
    }

    private IActionResult ToProblemResult(UpstreamServiceException exception)
    {
        var statusCode = exception.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => StatusCodes.Status400BadRequest,
            System.Net.HttpStatusCode.Conflict => StatusCodes.Status409Conflict,
            System.Net.HttpStatusCode.Unauthorized => StatusCodes.Status401Unauthorized,
            System.Net.HttpStatusCode.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status502BadGateway
        };

        return Problem(
            statusCode: statusCode,
            title: statusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status409Conflict => "Conflict",
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status403Forbidden => "Forbidden",
                _ => "Bad Gateway"
            },
            detail: exception.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = exception.Code,
                ["service"] = exception.ServiceName
            });
    }
}
