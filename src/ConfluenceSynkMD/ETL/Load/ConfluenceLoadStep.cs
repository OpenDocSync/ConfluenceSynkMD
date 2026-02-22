using System.Diagnostics;
using System.Linq;
using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.Models;
using ConfluenceSynkMD.Services;
using Serilog;

namespace ConfluenceSynkMD.ETL.Load;

/// <summary>
/// Load step (Upload direction): writes <see cref="TranslationBatchContext.TransformedDocuments"/>
/// to Confluence by creating/updating pages and uploading attachments.
/// Replaces the former <c>ConfluenceLoader</c>.
/// </summary>
public sealed class ConfluenceLoadStep : IPipelineStep
{
    private readonly IConfluenceApiClient _api;
    private readonly ILogger _logger;

    public string StepName => "ConfluenceLoad";

    public ConfluenceLoadStep(IConfluenceApiClient api, ILogger logger)
    {
        _api = api;
        _logger = logger.ForContext<ConfluenceLoadStep>();
    }

    public async Task<PipelineResult> ExecuteAsync(TranslationBatchContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Resolve space if not already done by an earlier step
            if (context.ResolvedSpace is null)
            {
                context.ResolvedSpace = await _api.GetSpaceByKeyAsync(context.Options.ConfluenceSpaceKey, ct);
            }

            var space = context.ResolvedSpace;
            _logger.Information("Resolved space '{Key}' → ID '{Id}'.", space.Key, space.Id);

            // Resolve root parent: explicit ID > root page by title > space homepage
            var rootParentId = context.Options.ConfluenceParentId;

            if (rootParentId is null && !string.IsNullOrWhiteSpace(context.Options.RootPage))
            {
                _logger.Information("Resolving --root-page '{Title}'...", context.Options.RootPage);
                var resolution = await _api.GetOrCreatePageUnderParentAsync(
                    context.Options.RootPage,
                    space.HomepageId ?? throw new InvalidOperationException(
                        $"Space '{space.Key}' has no homepage to create root page under."),
                    space.Id, ct);

                _logger.Information(
                    "RootPageDecision status={Status} title={Title} totalMatches={TotalMatches} matchesUnderParent={MatchesUnderParent}",
                    resolution.Status,
                    context.Options.RootPage,
                    resolution.TotalMatches,
                    resolution.MatchesUnderParent);

                if (resolution.Status == ConfluenceRootPageResolutionStatus.Ambiguous)
                {
                    return PipelineResult.Abort(
                        StepName,
                        $"Root page '{context.Options.RootPage}' is ambiguous in space '{space.Key}' " +
                        $"({resolution.TotalMatches} matches, {resolution.MatchesUnderParent} under homepage). " +
                        "Please resolve duplicates or use --conf-parent-id.");
                }

                rootParentId = resolution.Page?.Id
                    ?? throw new InvalidOperationException(
                        $"Root page '{context.Options.RootPage}' could not be resolved.");
                _logger.Information("Using root page '{Title}' (ID: {Id}).", context.Options.RootPage, rootParentId);
            }

            rootParentId ??= space.HomepageId
                ?? throw new InvalidOperationException(
                    $"Space '{context.Options.ConfluenceSpaceKey}' has no homepage and no --conf-parent-id / --root-page was specified.");

            // Pre-flight: verify title uniqueness across batch (Python parity — _process_items)
            var titleToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var doc in context.TransformedDocuments)
            {
                if (titleToPath.TryGetValue(doc.Title, out var existingPath))
                {
                    throw new InvalidOperationException(
                        $"Duplicate page title '{doc.Title}' in '{existingPath}' and '{doc.SourcePath}'. " +
                        "Each synchronized page must have a unique title.");
                }
                titleToPath[doc.Title] = doc.SourcePath;
            }

            foreach (var doc in context.TransformedDocuments)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await UploadDocumentAsync(doc, rootParentId, space.Id, context, ct);
                    context.LoadedCount++;
                }
                catch (Exception ex)
                {
                    context.FailedCount++;
                    _logger.Error(ex, "Failed to upload '{Title}'. Continuing with next document.", doc.Title);
                }
            }

            sw.Stop();

            var totalAttachments = context.TransformedDocuments.Sum(d => d.Attachments.Count);
            _logger.Information(
                "Upload complete: {SuccessCount}/{TotalCount} pages uploaded, " +
                "{AttachmentCount} attachments, {FailedCount} failed " +
                "({ElapsedMs:N0}ms, space '{SpaceKey}').",
                context.LoadedCount,
                context.TransformedDocuments.Count,
                totalAttachments,
                context.FailedCount,
                sw.ElapsedMilliseconds,
                context.Options.ConfluenceSpaceKey);

            if (context.LoadedCount == 0 && context.FailedCount > 0)
            {
                return PipelineResult.CriticalError(
                    StepName,
                    $"All {context.FailedCount} documents failed to upload.");
            }

            if (context.FailedCount > 0)
            {
                return PipelineResult.Warning(
                    StepName, context.LoadedCount, context.FailedCount, sw.Elapsed,
                    $"{context.LoadedCount} uploaded, {context.FailedCount} failed.");
            }

            return PipelineResult.Success(StepName, context.LoadedCount, sw.Elapsed);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            return PipelineResult.CriticalError(
                StepName,
                $"Upload failed: {ex.Message}",
                ex);
        }
    }

    private async Task UploadDocumentAsync(
        ConvertedDocument doc, string rootParentId, string spaceId,
        TranslationBatchContext context, CancellationToken ct)
    {
        ConfluencePage page;

        // Per-document space override (Python parity: space_key from frontmatter)
        var effectiveSpaceId = spaceId;
        var effectiveSpaceKey = context.Options.ConfluenceSpaceKey;
        if (doc.Metadata.SpaceKey is not null
            && !string.Equals(doc.Metadata.SpaceKey, context.Options.ConfluenceSpaceKey, StringComparison.OrdinalIgnoreCase))
        {
            var overrideSpace = await _api.GetSpaceByKeyAsync(doc.Metadata.SpaceKey, ct);
            effectiveSpaceId = overrideSpace.Id;
            effectiveSpaceKey = overrideSpace.Key;
            rootParentId = overrideSpace.HomepageId
                ?? throw new InvalidOperationException(
                    $"Override space '{doc.Metadata.SpaceKey}' has no homepage.");
            _logger.Information(
                "Using per-document space '{Key}' for '{Title}'.", effectiveSpaceKey, doc.Title);
        }

        // Resolve the parent page ID from hierarchy (only when --keep-hierarchy is active)
        var effectiveParentId = rootParentId;
        if (context.Options.KeepHierarchy
            && doc.ParentSourcePath is not null
            && context.PageIdCache.TryGetValue(doc.ParentSourcePath, out var cachedParentId))
        {
            effectiveParentId = cachedParentId;
            _logger.Debug("Resolved parent for '{Title}' → page ID '{ParentId}' (from '{ParentPath}').",
                doc.Title, cachedParentId, doc.ParentSourcePath);
        }

        // Determine parent ID: use explicit page_id from frontmatter, or lookup
        var explicitPageId = doc.Metadata.PageId;

        if (explicitPageId is not null)
        {
            // Update existing page
            var existing = await _api.GetPageByIdAsync(explicitPageId, ct);
            if (existing is not null)
            {
                var newVersion = (existing.Version?.Number ?? 0) + 1;
                page = await _api.UpdatePageAsync(
                    explicitPageId, doc.Title, doc.Content, newVersion, ct);
                _logger.Information("Updated page '{Title}' (ID: {Id}, v{Version}).",
                    doc.Title, page.Id, newVersion);
            }
            else
            {
                // Page ID specified but not found → create new
                page = await _api.CreatePageAsync(doc.Title, doc.Content, effectiveParentId, effectiveSpaceId, ct);
                _logger.Warning("Page ID '{Id}' not found, created new page '{Title}'.",
                    explicitPageId, doc.Title);
            }
        }
        else
        {
            // Lookup by title under the correct parent, or create
            var existing = await _api.GetPageByTitleAsync(doc.Title, effectiveSpaceId, ct);
            if (existing is not null)
            {
                // Ancestor verification: ensure the matched page is under our root (Python parity)
                if (!await IsTraceableToRootAsync(existing.Id, rootParentId, ct))
                {
                    _logger.Warning(
                        "Page '{Title}' (ID: {Id}) exists but is not under root page '{RootId}'. Creating new page instead.",
                        doc.Title, existing.Id, rootParentId);
                    existing = null; // fall through to create
                }
            }

            if (existing is not null)
            {
                // --skip-update: check if content has changed
                if (context.Options.SkipUpdate)
                {
                    var existingContent = existing.Body?.Storage?.Value ?? "";
                    if (existingContent == doc.Content)
                    {
                        _logger.Information("Skipping unchanged page '{Title}' (ID: {Id}).",
                            doc.Title, existing.Id);
                        page = existing;
                        goto PostUpdate;
                    }
                }

                var newVersion = (existing.Version?.Number ?? 0) + 1;
                page = await _api.UpdatePageAsync(
                    existing.Id, doc.Title, doc.Content, newVersion, ct);
                _logger.Information("Updated page '{Title}' (ID: {Id}, v{Version}).",
                    doc.Title, page.Id, newVersion);
            }
            else
            {
                page = await _api.CreatePageAsync(doc.Title, doc.Content, effectiveParentId, effectiveSpaceId, ct);
                _logger.Information("Created page '{Title}' (ID: {Id}) under parent '{ParentId}'.",
                    doc.Title, page.Id, effectiveParentId);
            }
        }

        PostUpdate:

        // Cache the page ID for potential child lookups
        context.PageIdCache[doc.SourcePath] = page.Id;
        context.SpaceKeyCache[doc.SourcePath] = effectiveSpaceKey;

        // Upload attachments
        foreach (var attachment in doc.Attachments)
        {
            try
            {
                await using var stream = File.OpenRead(attachment.AbsolutePath);
                await _api.UploadAttachmentAsync(page.Id, attachment.FileName, stream, attachment.MimeType, ct);
                _logger.Debug("Uploaded attachment '{File}' to page '{Id}'.",
                    attachment.FileName, page.Id);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to upload attachment '{File}' to page '{Title}'.",
                    attachment.FileName, doc.Title);
            }
        }
        // Sync labels from frontmatter tags
        if (doc.Metadata.Tags is { Count: > 0 })
        {
            try
            {
                var existingLabels = await _api.GetPageLabelsAsync(page.Id, ct);
                var labelsToAdd = doc.Metadata.Tags
                    .Where(t => !existingLabels.Contains(t, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (labelsToAdd.Count > 0)
                {
                    await _api.AddPageLabelsAsync(page.Id, labelsToAdd, ct);
                    _logger.Debug("Added {Count} label(s) to page '{Title}'.",
                        labelsToAdd.Count, doc.Title);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to sync labels for page '{Title}'.", doc.Title);
            }
        }

        // Set content properties for traceability
        if (doc.Metadata.Properties is { Count: > 0 })
        {
            foreach (var (key, value) in doc.Metadata.Properties)
            {
                try
                {
                    await _api.SetContentPropertyAsync(page.Id, key, value?.ToString() ?? "", ct);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to set property '{Key}' on page '{Title}'.",
                        key, doc.Title);
                }
            }
        }
    }

    /// <summary>
    /// Walks the parent chain from <paramref name="pageId"/> up to verify it is a
    /// descendant of <paramref name="rootId"/> (Python parity — ParentCatalog.is_traceable).
    /// </summary>
    private async Task<bool> IsTraceableToRootAsync(string pageId, string rootId, CancellationToken ct)
    {
        var currentId = pageId;
        const int maxDepth = 50; // safety limit
        for (var i = 0; i < maxDepth; i++)
        {
            if (currentId == rootId)
                return true;

            var page = await _api.GetPageByIdAsync(currentId, ct);
            if (page?.ParentId is null)
                return false;

            currentId = page.ParentId;
        }
        _logger.Warning("Ancestor chain exceeded {MaxDepth} levels for page '{Id}'.", maxDepth, pageId);
        return false;
    }
}
