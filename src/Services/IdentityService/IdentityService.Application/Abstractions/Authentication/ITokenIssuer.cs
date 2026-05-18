using IdentityService.Domain.Entities;

namespace IdentityService.Application.Abstractions.Authentication;

public interface ITokenIssuer
{
    AccessToken Issue(User user);
}
