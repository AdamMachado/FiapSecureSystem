using UploadService.Application.Abstractions.Clock;

namespace UploadService.Infrastructure.Clock;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
