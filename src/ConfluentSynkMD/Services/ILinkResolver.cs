namespace ConfluentSynkMD.Services;

/// <summary>
/// Classifies and resolves internal Markdown links to their Confluence targets.
/// Centralizes link resolution logic that was previously scattered across the renderer (TKT-005).
/// </summary>
public interface ILinkResolver
{
    /// <summary>
    /// Resolves a link URL to its Confluence representation.
    /// </summary>
    /// <param name="linkUrl">Raw link URL from the Markdown source (e.g. "guides/setup.md#install").</param>
    /// <param name="webUiMode">Whether --webui-links is active.</param>
    /// <param name="sourcePath">Optional source document path (relative to sync root) for contextual relative link resolution.</param>
    LinkResolution Resolve(string linkUrl, bool webUiMode, string? sourcePath = null);
}

/// <summary>Result of resolving a link.</summary>
public record LinkResolution(
    LinkType Type,
    string ResolvedTitle,
    string? DisplayUrl,
    string? Fragment,
    bool IsResolved);

/// <summary>Classification of link targets.</summary>
public enum LinkType
{
    /// <summary>http://, https://, mailto: links.</summary>
    External,
    /// <summary>Internal .md link to another page.</summary>
    InternalPage,
    /// <summary>Same-page anchor (#section).</summary>
    Anchor,
    /// <summary>Relative non-.md file (treated as attachment).</summary>
    Attachment
}
