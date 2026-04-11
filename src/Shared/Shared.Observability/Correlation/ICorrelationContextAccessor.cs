
namespace Shared.Observability.Correlation
{
    public interface ICorrelationContextAccessor
    {
        string? CausationId { get; set; }
        string? CorrelationId { get; set; }

        void Clear();
        Guid? GetCausationGuidOrNull();
        Guid GetOrCreateCorrelationGuid();
    }
}