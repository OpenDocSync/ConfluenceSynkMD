namespace ConfluenceSynkMD.Models;

/// <summary>
/// Enriched page model that bundles a Confluence page with its attachment metadata.
/// Used in the download pipeline so the Transform and Load stages have access
/// to attachment information for downloading images.
/// </summary>
public record ConfluencePageWithAttachments(
    ConfluencePage Page,
    IReadOnlyList<ConfluenceAttachment> Attachments,
    string? ParentPageId = null,
    int Depth = 0,
    bool HasChildren = false);
