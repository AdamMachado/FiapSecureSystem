namespace ProcessingService.Infrastructure.AI.Policies;

public sealed class AiServiceTierPolicy
{
    private static readonly HashSet<string> SupportedTiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "auto",
        "default",
        "flex",
        "priority"
    };

    public string Normalize(string? serviceTier)
    {
        if (string.IsNullOrWhiteSpace(serviceTier))
            return "auto";

        var normalized = serviceTier.Trim().ToLowerInvariant();
        return SupportedTiers.Contains(normalized) ? normalized : "auto";
    }

    public string GetFallbackTier(string currentTier)
    {
        return currentTier.Equals("flex", StringComparison.OrdinalIgnoreCase) ? "auto" : currentTier;
    }
}
