namespace ConfluenceSynkMD.Services;

/// <summary>
/// Builds Confluence Web UI URLs for internal page links.
/// </summary>
public interface IConfluenceUrlBuilder
{
    /// <summary>Configured strategy identifier (e.g. "space-title" or "page-id").</summary>
    string Strategy { get; }

    /// <summary>
    /// Builds a URL for a Confluence page link.
    /// </summary>
    string BuildPageUrl(string title, string? spaceKey, string? fragment, string? pageId);
}
