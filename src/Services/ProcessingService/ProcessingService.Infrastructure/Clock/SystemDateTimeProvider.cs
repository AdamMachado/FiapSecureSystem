using ProcessingService.Application.Abstractions.Clock;

namespace ProcessingService.Infrastructure.Clock;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
