using ConfluenceSynkMD.Models;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Abstraction for the Confluence Cloud REST API.
/// Supports both API v2 (pages) and v1 (attachments).
/// Mirrors md2conf's ConfluenceSession from api.py.
/// </summary>
public interface IConfluenceApiClient
{
    // ─── Space ──────────────────────────────────────────────────────────────
    Task<ConfluenceSpace> GetSpaceByKeyAsync(string spaceKey, CancellationToken ct = default);

    // ─── Pages ──────────────────────────────────────────────────────────────
    Task<ConfluencePage?> GetPageByIdAsync(string pageId, CancellationToken ct = default);
    Task<ConfluencePage?> GetPageByTitleAsync(string title, string spaceId, CancellationToken ct = default);
    IAsyncEnumerable<ConfluencePage> GetChildPagesAsync(string parentId, CancellationToken ct = default);
    IAsyncEnumerable<ConfluencePage> GetPagesInSpaceAsync(string spaceId, CancellationToken ct = default);

    Task<ConfluencePage> CreatePageAsync(
        string title, string content, string parentId, string spaceId,
        CancellationToken ct = default);

    Task<ConfluencePage> UpdatePageAsync(
        string pageId, string title, string content, int newVersion,
        CancellationToken ct = default);



    /// <summary>
    /// Finds by title or creates a new page. Analogous to md2conf's get_or_create_page.
    /// </summary>
    Task<ConfluencePage> GetOrCreatePageAsync(
        string title, string parentId, string spaceId,
        CancellationToken ct = default);

    /// <summary>
    /// Finds a page with <paramref name="title"/> under the given <paramref name="parentId"/>,
    /// creates one if no unambiguous match exists under that parent.
    /// Returns an explicit status to avoid hidden best-effort behavior on ambiguity.
    /// </summary>
    Task<ConfluenceRootPageResolution> GetOrCreatePageUnderParentAsync(
        string title, string parentId, string spaceId,
        CancellationToken ct = default);

    // ─── Attachments ────────────────────────────────────────────────────────

    Task UploadAttachmentAsync(
        string pageId, string fileName, Stream content, string mimeType,
        CancellationToken ct = default);

    /// <summary>Lists all attachments on a page.</summary>
    IAsyncEnumerable<ConfluenceAttachment> GetAttachmentsAsync(
        string pageId, CancellationToken ct = default);

    /// <summary>Downloads attachment binary content by its download path.</summary>
    Task<Stream> DownloadAttachmentAsync(string downloadPath, CancellationToken ct = default);

    // ─── Labels ─────────────────────────────────────────────────────────────

    /// <summary>Gets all labels for a page.</summary>
    Task<IReadOnlyList<string>> GetPageLabelsAsync(string pageId, CancellationToken ct = default);

    /// <summary>Adds labels to a page.</summary>
    Task AddPageLabelsAsync(string pageId, IEnumerable<string> labels, CancellationToken ct = default);



    // ─── Content Properties ─────────────────────────────────────────────────

    /// <summary>Sets a content property (key-value) on a page.</summary>
    Task SetContentPropertyAsync(
        string pageId, string key, string value, CancellationToken ct = default);


}

public enum ConfluenceRootPageResolutionStatus
{
    FoundUnderParent,
    Created,
    Ambiguous
}

public sealed record ConfluenceRootPageResolution(
    ConfluencePage? Page,
    ConfluenceRootPageResolutionStatus Status,
    int TotalMatches,
    int MatchesUnderParent);
