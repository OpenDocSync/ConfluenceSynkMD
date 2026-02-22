namespace ConfluentSynkMD.Models;

/// <summary>
/// Holds all CLI-provided synchronization options.
/// Maps positional/flag arguments from both C# CLI and Python md2conf.
/// </summary>
/// <param name="Mode">Upload, Download, or LocalExport direction.</param>
/// <param name="Path">Local filesystem path (repo root or subfolder).</param>
/// <param name="ConfluenceSpaceKey">Confluence Space Key (e.g. "DEV").</param>
/// <param name="ConfluenceParentId">Optional parent page ID for subtree upload.</param>
/// <param name="RootPage">Optional root page title to upload under (alternative to parent ID).</param>
/// <param name="KeepHierarchy">Whether to preserve local directory hierarchy in Confluence.</param>
/// <param name="SkipUpdate">Skip uploading pages whose content has not changed.</param>
/// <param name="LocalOnly">When true, only produce local CSF output without API calls.</param>
/// <param name="NoWriteBack">When true, do not write Confluence Page-ID back into Markdown frontmatter.</param>
/// <param name="LogLevel">Logging verbosity: debug, info, warning, error, critical.</param>
public record SyncOptions(
    SyncMode Mode,
    string Path,
    string ConfluenceSpaceKey,
    string? ConfluenceParentId = null,
    string? RootPage = null,
    bool KeepHierarchy = true,
    bool SkipUpdate = false,
    bool LocalOnly = false,
    bool NoWriteBack = false,
    string LogLevel = "info");
