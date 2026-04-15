using Shared.Kernel.Primitives;
using FiapSecureSystem.ReportService.Domain.Exceptions;

namespace FiapSecureSystem.ReportService.Domain.ValueObjects;

public sealed class ReportContent : ValueObject
{
    public string Summary { get; }
    public IReadOnlyCollection<string> Components { get; }
    public IReadOnlyCollection<string> Risks { get; }
    public IReadOnlyCollection<string> Recommendations { get; }

    private ReportContent(
        string summary,
        IReadOnlyCollection<string> components,
        IReadOnlyCollection<string> risks,
        IReadOnlyCollection<string> recommendations)
    {
        Summary = summary;
        Components = components;
        Risks = risks;
        Recommendations = recommendations;
    }

    public static ReportContent Create(
        string summary,
        IEnumerable<string> components,
        IEnumerable<string> risks,
        IEnumerable<string> recommendations)
    {
        if (string.IsNullOrWhiteSpace(summary))
            throw new EmptyReportContentException();

        var componentList = components?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList() ?? [];

        var riskList = risks?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList() ?? [];

        var recommendationList = recommendations?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList() ?? [];

        if (componentList.Count == 0 && riskList.Count == 0 && recommendationList.Count == 0)
            throw new EmptyReportContentException();

        return new ReportContent(summary.Trim(), componentList, riskList, recommendationList);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Summary;

        foreach (var component in Components)
            yield return component;

        foreach (var risk in Risks)
            yield return risk;

        foreach (var recommendation in Recommendations)
            yield return recommendation;
    }
}