using ReportService.Application.Abstractions.Clock;

namespace ReportService.Infrastructure.Clock;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}