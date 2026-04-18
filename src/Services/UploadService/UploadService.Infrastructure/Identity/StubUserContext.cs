using UploadService.Application.Abstractions.Identity;

namespace UploadService.Infrastructure.Identity;

public sealed class StubUserContext : IUserContext
{
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid GetRequiredUserId() => DefaultUserId;
}
