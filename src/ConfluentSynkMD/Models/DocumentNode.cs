namespace ConfluentSynkMD.Models;

/// <summary>
/// Represents a Markdown document within the file tree, analogous to
/// md2conf's DocumentNode from processor.py.
/// </summary>
/// <param name="AbsolutePath">Absolute path to the .md file.</param>
/// <param name="RelativePath">Path relative to the sync root.</param>
/// <param name="Metadata">Parsed frontmatter and inline metadata.</param>
/// <param name="MarkdownContent">Raw Markdown text (frontmatter stripped).</param>
/// <param name="Children">Child document nodes (for directory-based hierarchy).</param>
public record DocumentNode(
    string AbsolutePath,
    string RelativePath,
    DocumentMetadata Metadata,
    string MarkdownContent,
    IReadOnlyList<DocumentNode> Children,
    string? ParentSourcePath = null);
