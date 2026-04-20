using UploadService.Application.Abstractions.Clock;

namespace UploadService.Api.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
