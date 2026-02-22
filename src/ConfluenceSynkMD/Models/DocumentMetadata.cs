using ConfluenceSynkMD.Configuration;

namespace ConfluenceSynkMD.Models;

/// <summary>
/// Metadata extracted from YAML frontmatter and inline HTML comments.
/// Mirrors md2conf's DocumentProperties from scanner.py.
/// </summary>
/// <param name="PageId">Confluence page ID (from frontmatter or &lt;!-- confluence-page-id --&gt;).</param>
/// <param name="SpaceKey">Confluence space key override.</param>
/// <param name="Title">Explicit title from frontmatter (overrides H1 extraction).</param>
/// <param name="Tags">Content labels / tags for the Confluence page.</param>
/// <param name="Synchronized">Whether this document should be synced (default true).</param>
/// <param name="GeneratedBy">Tool identifier injected as a comment.</param>
/// <param name="Properties">Arbitrary key-value pairs for Confluence page properties.</param>
/// <param name="LayoutOverride">Per-document layout override from YAML frontmatter.</param>
public record DocumentMetadata(
    string? PageId = null,
    string? SpaceKey = null,
    string? Title = null,
    IReadOnlyList<string>? Tags = null,
    bool Synchronized = true,
    string? GeneratedBy = null,
    IReadOnlyDictionary<string, object>? Properties = null,
    LayoutOptions? LayoutOverride = null);
