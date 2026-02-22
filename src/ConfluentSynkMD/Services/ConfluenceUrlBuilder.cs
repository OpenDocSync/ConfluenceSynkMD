namespace ConfluentSynkMD.Services;

/// <summary>
/// Default implementation for Confluence Web UI link generation.
/// </summary>
public sealed class ConfluenceUrlBuilder : IConfluenceUrlBuilder
{
    public const string StrategySpaceTitle = "space-title";
    public const string StrategyPageId = "page-id";

    public string Strategy { get; }

    public ConfluenceUrlBuilder(string? strategy = null)
    {
        Strategy = string.IsNullOrWhiteSpace(strategy)
            ? StrategySpaceTitle
            : strategy.Trim().ToLowerInvariant();
    }

    public string BuildPageUrl(string title, string? spaceKey, string? fragment, string? pageId)
    {
        var suffix = string.IsNullOrEmpty(fragment) ? string.Empty : $"#{fragment}";

        if (Strategy == StrategyPageId && !string.IsNullOrWhiteSpace(pageId))
        {
            return $"/wiki/pages/viewpage.action?pageId={Uri.EscapeDataString(pageId)}{suffix}";
        }

        var encodedTitle = Uri.EscapeDataString(title);
        return !string.IsNullOrWhiteSpace(spaceKey)
            ? $"/wiki/display/{Uri.EscapeDataString(spaceKey)}/{encodedTitle}{suffix}"
            : $"/wiki/display/{encodedTitle}{suffix}";
    }
}
