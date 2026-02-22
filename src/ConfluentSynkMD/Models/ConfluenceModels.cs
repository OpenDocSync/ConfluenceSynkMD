using System.Text.Json.Serialization;

namespace ConfluentSynkMD.Models;

// ─── Confluence REST API v2 DTOs ────────────────────────────────────────────
// Mirrors md2conf's dataclasses from api.py

/// <summary>Represents a Confluence page.</summary>
public record ConfluencePage(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("spaceId")] string SpaceId,
    [property: JsonPropertyName("parentId")] string? ParentId,
    [property: JsonPropertyName("body")] ConfluencePageBody? Body,
    [property: JsonPropertyName("version")] ConfluenceVersion? Version);

/// <summary>Page body wrapper.</summary>
public record ConfluencePageBody(
    [property: JsonPropertyName("storage")] ConfluenceStorage? Storage);

/// <summary>Storage format representation (XHTML).</summary>
public record ConfluenceStorage(
    [property: JsonPropertyName("representation")] string Representation,
    [property: JsonPropertyName("value")] string Value);

/// <summary>Version information for optimistic concurrency.</summary>
public record ConfluenceVersion(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("message")] string? Message = null);

/// <summary>Confluence space metadata.</summary>
public record ConfluenceSpace(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("homepageId")] string? HomepageId = null);

/// <summary>Confluence attachment metadata.</summary>
public record ConfluenceAttachment(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("mediaType")] string MediaType,
    [property: JsonPropertyName("fileSize")] long FileSize,
    [property: JsonPropertyName("_links")] ConfluenceAttachmentLinks? Links = null);

/// <summary>Attachment links containing the download path.</summary>
public record ConfluenceAttachmentLinks(
    [property: JsonPropertyName("download")] string? Download);

/// <summary>Generic paginated result from Confluence REST API v2.</summary>
public record ConfluencePaginatedResult<T>(
    [property: JsonPropertyName("results")] IReadOnlyList<T> Results,
    [property: JsonPropertyName("_links")] ConfluenceLinks? Links);

/// <summary>Pagination links.</summary>
public record ConfluenceLinks(
    [property: JsonPropertyName("next")] string? Next);

// ─── Request DTOs ───────────────────────────────────────────────────────────

/// <summary>Create page request body (API v2).</summary>
public record CreatePageRequest(
    [property: JsonPropertyName("spaceId")] string SpaceId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("parentId")] string? ParentId,
    [property: JsonPropertyName("body")] ConfluencePageBody Body,
    [property: JsonPropertyName("status")] string Status = "current");

/// <summary>Update page request body (API v2).</summary>
public record UpdatePageRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("body")] ConfluencePageBody Body,
    [property: JsonPropertyName("version")] ConfluenceVersion Version,
    [property: JsonPropertyName("status")] string Status = "current");

// ─── Labels & Properties ────────────────────────────────────────────────────

/// <summary>Confluence page label.</summary>
public record ConfluenceLabel(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("prefix")] string Prefix);

/// <summary>Confluence content property.</summary>
public record ConfluenceProperty(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("version")] ConfluenceVersion Version);

/// <summary>Paginated container for content properties.</summary>
public record ConfluencePropertyResult(
    [property: JsonPropertyName("results")] IReadOnlyList<ConfluenceProperty> Results);
