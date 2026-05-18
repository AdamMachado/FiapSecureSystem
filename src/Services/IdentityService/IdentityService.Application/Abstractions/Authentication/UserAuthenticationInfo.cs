using IdentityService.Domain.Entities;

namespace IdentityService.Application.Abstractions.Authentication;

public sealed record UserAuthenticationInfo(
    User User,
    string PasswordHash);
