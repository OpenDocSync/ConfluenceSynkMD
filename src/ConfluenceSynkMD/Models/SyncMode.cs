namespace ConfluenceSynkMD.Models;

/// <summary>
/// Defines the synchronization direction.
/// </summary>
public enum SyncMode
{
    /// <summary>Upload local Markdown files to Confluence.</summary>
    Upload,

    /// <summary>Download Confluence pages as local Markdown files.</summary>
    Download,

    /// <summary>Markdown → local Confluence Storage Format files (no API calls).</summary>
    LocalExport
}
