using Shared.Kernel.Exceptions;
using Shared.Kernel.Primitives;

namespace ProcessingService.Domain.ValueObjects;

public sealed class ProcessingResultSummary : ValueObject
{
    private readonly IReadOnlyCollection<string> _warnings;

    public string Overview { get; }
    public int TotalComponents { get; }
    public int TotalRisks { get; }
    public int TotalRecommendations { get; }
    public bool RequiresManualReview { get; }
    public IReadOnlyCollection<string> Warnings => _warnings;

    private ProcessingResultSummary(
        string overview,
        int totalComponents,
        int totalRisks,
        int totalRecommendations,
        bool requiresManualReview,
        IReadOnlyCollection<string> warnings)
    {
        Overview = overview;
        TotalComponents = totalComponents;
        TotalRisks = totalRisks;
        TotalRecommendations = totalRecommendations;
        RequiresManualReview = requiresManualReview;
        _warnings = warnings;
    }

    public static ProcessingResultSummary Create(
        string overview,
        int totalComponents,
        int totalRisks,
        int totalRecommendations,
        bool requiresManualReview,
        IReadOnlyCollection<string>? warnings = null)
    {
        if (string.IsNullOrWhiteSpace(overview))
            throw new ValidationException("Processing result overview is required.");

        if (totalComponents < 0)
            throw new ValidationException("Total components cannot be negative.");

        if (totalRisks < 0)
            throw new ValidationException("Total risks cannot be negative.");

        if (totalRecommendations < 0)
            throw new ValidationException("Total recommendations cannot be negative.");

        var normalizedWarnings = (warnings ?? Array.Empty<string>())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ProcessingResultSummary(
            overview.Trim(),
            totalComponents,
            totalRisks,
            totalRecommendations,
            requiresManualReview,
            normalizedWarnings);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Overview;
        yield return TotalComponents;
        yield return TotalRisks;
        yield return TotalRecommendations;
        yield return RequiresManualReview;

        foreach (var warning in _warnings)
            yield return warning;
    }
}
