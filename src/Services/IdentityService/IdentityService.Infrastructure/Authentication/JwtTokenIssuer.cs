using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Application.Abstractions.Authentication;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Security.Authentication;

namespace IdentityService.Infrastructure.Authentication;

public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly JwtOptions _jwtOptions;
    private readonly IdentityOptions _identityOptions;

    public JwtTokenIssuer(
        IOptions<JwtOptions> jwtOptions,
        IOptions<IdentityOptions> identityOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _identityOptions = identityOptions.Value;
    }

    public AccessToken Issue(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_identityOptions.AccessTokenExpirationMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new("scope", string.Join(' ', user.Scopes))
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresInSeconds = (long)Math.Max(0, (expiresAtUtc - DateTime.UtcNow).TotalSeconds);

        return new AccessToken(serializedToken, expiresAtUtc, expiresInSeconds);
    }
}
