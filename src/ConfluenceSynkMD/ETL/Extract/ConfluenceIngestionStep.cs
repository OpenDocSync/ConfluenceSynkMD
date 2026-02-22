using System.Diagnostics;
using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.Models;
using ConfluenceSynkMD.Services;
using Serilog;

namespace ConfluenceSynkMD.ETL.Extract;

/// <summary>
/// Extract step: reads pages from Confluence via REST API and populates
/// <see cref="TranslationBatchContext.ExtractedConfluencePages"/>.
/// Also resolves and caches the Confluence space in <see cref="TranslationBatchContext.ResolvedSpace"/>.
/// Replaces the former <c>ConfluenceExtractor</c> streaming implementation.
/// </summary>
public sealed class ConfluenceIngestionStep : IPipelineStep
{
    private readonly IConfluenceApiClient _api;
    private readonly ILogger _logger;

    public string StepName => "ConfluenceIngestion";

    public ConfluenceIngestionStep(IConfluenceApiClient api, ILogger logger)
    {
        _api = api;
        _logger = logger.ForContext<ConfluenceIngestionStep>();
    }

    public async Task<PipelineResult> ExecuteAsync(TranslationBatchContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var space = await _api.GetSpaceByKeyAsync(context.Options.ConfluenceSpaceKey, ct);
            context.ResolvedSpace = space;
            _logger.Information("Extracting pages from space '{Key}' (ID: {Id}).",
                space.Key, space.Id);

            var count = 0;

            // Resolve effective parent ID: --conf-parent-id > --root-page > (space-wide)
            var parentId = context.Options.ConfluenceParentId;
            if (parentId is null && context.Options.RootPage is not null)
            {
                var rootPage = await _api.GetPageByTitleAsync(
                    context.Options.RootPage, space.Id, ct);
                if (rootPage is null)
                {
                    return PipelineResult.Abort(StepName,
                        $"Root page '{context.Options.RootPage}' not found in space '{space.Key}'.");
                }
                parentId = rootPage.Id;
                _logger.Information("Resolved --root-page '{Title}' → ID '{Id}'.",
                    context.Options.RootPage, parentId);
            }

            if (parentId is not null)
            {
                _logger.Information("Fetching subtree under parent '{Id}'.", parentId);

                // Include the root page itself as a document (round-trip parity)
                var rootEnriched = await EnrichWithAttachmentsAsync(parentId, ct);
                if (rootEnriched is not null)
                {
                    context.ExtractedConfluencePages.Add(rootEnriched with
                    {
                        Depth = 0,
                        HasChildren = true  // root is always treated as having children
                    });
                    count++;
                }

                await foreach (var page in FetchSubtreeAsync(parentId, parentId, 1, ct))
                {
                    context.ExtractedConfluencePages.Add(page);
                    count++;
                }
            }
            else
            {
                await foreach (var pageSummary in _api.GetPagesInSpaceAsync(space.Id, ct))
                {
                    var enriched = await EnrichWithAttachmentsAsync(pageSummary.Id, ct);
                    if (enriched is not null)
                    {
                        context.ExtractedConfluencePages.Add(enriched);
                        count++;
                    }
                }
            }

            sw.Stop();

            if (count == 0)
            {
                return PipelineResult.Abort(
                    StepName,
                    $"No pages found in space '{context.Options.ConfluenceSpaceKey}'.");
            }

            _logger.Information("Extracted {Count} Confluence page(s).", count);
            return PipelineResult.Success(StepName, count, sw.Elapsed);
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
                $"Failed to extract Confluence pages: {ex.Message}",
                ex);
        }
    }

    /// <summary>Recursively fetches all child pages under a parent, with full body + attachments + hierarchy info.</summary>
    private async IAsyncEnumerable<ConfluencePageWithAttachments> FetchSubtreeAsync(
        string parentId, string? parentPageId, int depth,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        // Collect children first to determine which pages have children
        var childIds = new List<string>();
        await foreach (var childSummary in _api.GetChildPagesAsync(parentId, ct))
        {
            childIds.Add(childSummary.Id);
        }

        foreach (var childId in childIds)
        {
            // Check if this child has its own children
            var grandchildren = new List<string>();
            await foreach (var gc in _api.GetChildPagesAsync(childId, ct))
            {
                grandchildren.Add(gc.Id);
            }
            var hasChildren = grandchildren.Count > 0;

            var enriched = await EnrichWithAttachmentsAsync(childId, ct);
            if (enriched is not null)
            {
                yield return enriched with
                {
                    ParentPageId = parentPageId,
                    Depth = depth,
                    HasChildren = hasChildren
                };
            }

            // Recursively fetch grandchildren
            if (hasChildren)
            {
                await foreach (var grandchild in FetchSubtreeAsync(childId, childId, depth + 1, ct))
                {
                    yield return grandchild;
                }
            }
        }
    }

    /// <summary>Fetches full page content + attachment list for a given page ID.</summary>
    private async Task<ConfluencePageWithAttachments?> EnrichWithAttachmentsAsync(
        string pageId, CancellationToken ct)
    {
        var fullPage = await _api.GetPageByIdAsync(pageId, ct);
        if (fullPage is null) return null;

        var attachments = new List<ConfluenceAttachment>();
        await foreach (var att in _api.GetAttachmentsAsync(pageId, ct))
        {
            attachments.Add(att);
        }

        _logger.Debug("Extracted page '{Title}' (ID: {Id}) with {Count} attachments.",
            fullPage.Title, fullPage.Id, attachments.Count);

        return new ConfluencePageWithAttachments(fullPage, attachments);
    }
}
