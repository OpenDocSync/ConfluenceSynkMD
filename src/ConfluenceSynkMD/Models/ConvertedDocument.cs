namespace ConfluenceSynkMD.Models;

/// <summary>
/// A document that has been transformed and is ready for loading.
/// Content is XHTML (upload direction) or Markdown (download direction).
/// </summary>
/// <param name="Title">Page title for Confluence.</param>
/// <param name="Content">Transformed content (Confluence Storage Format XHTML or Markdown).</param>
/// <param name="Metadata">Original metadata from the source document.</param>
/// <param name="SourcePath">Path to the original source file.</param>
/// <param name="Attachments">Local image/file attachments to upload.</param>
public record ConvertedDocument(
    string Title,
    string Content,
    DocumentMetadata Metadata,
    string SourcePath,
    IReadOnlyList<AttachmentInfo> Attachments,
    string? ParentSourcePath = null,
    string? ParentPageId = null,
    bool HasChildren = false,
    string? OriginalFilename = null,
    string? OriginalSourcePath = null);

/// <summary>
/// Describes a local file that should be uploaded as a Confluence attachment.
/// </summary>
/// <param name="FileName">Attachment filename (basename).</param>
/// <param name="AbsolutePath">Local filesystem path to the file.</param>
/// <param name="MimeType">MIME type of the attachment.</param>
public record AttachmentInfo(string FileName, string AbsolutePath, string MimeType);
