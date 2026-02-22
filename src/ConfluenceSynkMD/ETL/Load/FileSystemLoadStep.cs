using System.Diagnostics;
using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.Models;
using ConfluenceSynkMD.Services;
using Serilog;

namespace ConfluenceSynkMD.ETL.Load;

/// <summary>
/// Load step (Download direction): writes <see cref="TranslationBatchContext.TransformedDocuments"/>
/// to the local filesystem, reconstructing the directory hierarchy.
/// Pages with children get a subdirectory with an index.md.
/// Leaf pages are written as &lt;slug&gt;.md in their parent's directory.
/// Replaces the former <c>FileSystemLoader</c>.
/// </summary>
public sealed class FileSystemLoadStep : IPipelineStep
{
    private readonly SlugGenerator _slugGenerator;
    private readonly IConfluenceApiClient _api;
    private readonly ILogger _logger;

    public string StepName => "FileSystemLoad";

    public FileSystemLoadStep(
        SlugGenerator slugGenerator,
        IConfluenceApiClient api,
        ILogger logger)
    {
        _slugGenerator = slugGenerator;
        _api = api;
        _logger = logger.ForContext<FileSystemLoadStep>();
    }

    public async Task<PipelineResult> ExecuteAsync(TranslationBatchContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var rootPath = context.Options.Path;
        Directory.CreateDirectory(rootPath);

        var downloadedAttachments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var doc in context.TransformedDocuments)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await SaveDocumentAsync(doc, rootPath, downloadedAttachments, context, ct);
                context.LoadedCount++;
            }
            catch (Exception ex)
            {
                context.FailedCount++;
                _logger.Error(ex, "Failed to save '{Title}'. Continuing.", doc.Title);
            }
        }

        sw.Stop();

        _logger.Information("Download complete: {Success} saved, {Errors} failed.",
            context.LoadedCount, context.FailedCount);

        if (context.LoadedCount == 0 && context.FailedCount > 0)
        {
            return PipelineResult.CriticalError(
                StepName,
                $"All {context.FailedCount} documents failed to save.");
        }

        if (context.FailedCount > 0)
        {
            return PipelineResult.Warning(
                StepName, context.LoadedCount, context.FailedCount, sw.Elapsed,
                $"{context.LoadedCount} saved, {context.FailedCount} failed.");
        }

        return PipelineResult.Success(StepName, context.LoadedCount, sw.Elapsed);
    }

    private async Task SaveDocumentAsync(
        ConvertedDocument doc,
        string rootPath,
        HashSet<string> downloadedAttachments,
        TranslationBatchContext context,
        CancellationToken ct)
    {
        // ── Strategy 1: Use persisted source-path for exact round-trip reconstruction ──
        if (doc.OriginalSourcePath is not null)
        {
            var normalizedPath = doc.OriginalSourcePath.Replace('/', Path.DirectorySeparatorChar);
            var targetPath = Path.Combine(rootPath, normalizedPath);
            var targetDir = Path.GetDirectoryName(targetPath)!;
            Directory.CreateDirectory(targetDir);

            await File.WriteAllTextAsync(targetPath, doc.Content, ct);
            _logger.Information("Saved '{Title}' → {Path} (from source-path)", doc.Title, targetPath);

            // Register for child lookups
            if (doc.Metadata.PageId is not null)
                context.PageIdCache[doc.Metadata.PageId] = targetDir;

            await DownloadAttachmentsAsync(doc, targetDir, downloadedAttachments, ct);
            return;
        }

        // ── Strategy 2: Slug-based fallback (legacy pages without metadata) ──
        _logger.Debug("No source-path metadata for '{Title}', using slug fallback.", doc.Title);

        // Root page without any source mapping: don't write as file, only register for child lookups
        if (doc.ParentPageId is null && doc.HasChildren && doc.OriginalFilename is null)
        {
            if (doc.Metadata.PageId is not null)
                context.PageIdCache[doc.Metadata.PageId] = rootPath;
            _logger.Information("Skipped root page '{Title}' (no source-path, virtual root).", doc.Title);
            return;
        }

        var slug = SlugGenerator.GenerateSlug(doc.Title);
        // Use original filename from metadata if available, else fall back to slug
        var fileBaseName = doc.OriginalFilename is not null
            ? Path.GetFileNameWithoutExtension(doc.OriginalFilename)
            : slug;

        // Determine the parent directory
        string parentDir;
        if (doc.ParentPageId is not null && context.PageIdCache.TryGetValue(doc.ParentPageId, out var parentPath))
        {
            parentDir = parentPath;
        }
        else
        {
            parentDir = rootPath;
        }

        string filePath;
        string docDir;

        if (doc.HasChildren)
        {
            // This page has children → create a subdirectory and write as index.md
            docDir = Path.Combine(parentDir, slug);
            Directory.CreateDirectory(docDir);
            filePath = Path.Combine(docDir, "index.md");

            // Register directory for children to find
            if (doc.Metadata.PageId is not null)
                context.PageIdCache[doc.Metadata.PageId] = docDir;
        }
        else
        {
            // Leaf page → write as <fileBaseName>.md in parent directory
            docDir = parentDir;
            filePath = Path.Combine(parentDir, $"{fileBaseName}.md");

            // Handle filename collisions
            var counter = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(parentDir, $"{fileBaseName}-{counter++}.md");
            }
        }

        await File.WriteAllTextAsync(filePath, doc.Content, ct);
        _logger.Information("Saved '{Title}' → {Path}", doc.Title, filePath);

        await DownloadAttachmentsAsync(doc, docDir, downloadedAttachments, ct);
    }

    /// <summary>Downloads attachments into img/ inside the given directory.</summary>
    private async Task DownloadAttachmentsAsync(
        ConvertedDocument doc, string docDir,
        HashSet<string> downloadedAttachments,
        CancellationToken ct)
    {
        if (doc.Attachments.Count == 0) return;

        var imgDir = Path.Combine(docDir, "img");
        Directory.CreateDirectory(imgDir);

        foreach (var att in doc.Attachments)
        {
            var attKey = $"{docDir}|{att.FileName}";
            if (!downloadedAttachments.Add(attKey))
            {
                _logger.Debug("Attachment '{File}' already downloaded for this directory, skipping.", att.FileName);
                continue;
            }

            var imgPath = Path.Combine(imgDir, att.FileName);
            if (File.Exists(imgPath))
            {
                _logger.Debug("Attachment '{File}' already exists on disk, skipping.", att.FileName);
                continue;
            }

            try
            {
                await using var stream = await _api.DownloadAttachmentAsync(att.AbsolutePath, ct);
                await using var fileStream = File.Create(imgPath);
                await stream.CopyToAsync(fileStream, ct);
                _logger.Information("Downloaded attachment '{File}' → {Path}", att.FileName, imgPath);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to download attachment '{File}'. Continuing.", att.FileName);
            }
        }
    }
}
