using System.Diagnostics;
using System.Text.RegularExpressions;
using ConfluentSynkMD.ETL.Core;
using ConfluentSynkMD.Models;
using Serilog;

namespace ConfluentSynkMD.ETL.Load;

/// <summary>
/// Pipeline step that writes Confluence page-id and space-key back into the
/// original Markdown source files as HTML comments (Python parity — _update_markdown).
/// Runs after <see cref="ConfluenceLoadStep"/>.
/// Controlled by <see cref="SyncOptions.NoWriteBack"/>.
/// </summary>
public sealed partial class WriteBackStep : IPipelineStep
{
    private readonly ILogger _logger;

    public string StepName => "WriteBack";

    public WriteBackStep(ILogger logger)
    {
        _logger = logger.ForContext<WriteBackStep>();
    }

    public async Task<PipelineResult> ExecuteAsync(TranslationBatchContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        if (context.Options.NoWriteBack)
        {
            _logger.Information("Write-back disabled by --no-write-back flag.");
            return PipelineResult.Success(StepName, 0, TimeSpan.Zero);
        }

        var count = 0;
        foreach (var doc in context.TransformedDocuments)
        {
            ct.ThrowIfCancellationRequested();

            if (!context.PageIdCache.TryGetValue(doc.SourcePath, out var pageId))
            {
                _logger.Debug("No cached page ID for '{Path}', skipping write-back.", doc.SourcePath);
                continue;
            }

            try
            {
                var effectiveSpaceKey = context.SpaceKeyCache.GetValueOrDefault(doc.SourcePath)
                                     ?? context.Options.ConfluenceSpaceKey;
                await WritePageIdToMarkdownAsync(doc.SourcePath, pageId, effectiveSpaceKey, ct);
                count++;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to write back page ID to '{Path}'.", doc.SourcePath);
            }
        }

        sw.Stop();
        _logger.Information("Write-back complete: {Count} file(s) updated.", count);
        return PipelineResult.Success(StepName, count, sw.Elapsed);
    }

    /// <summary>
    /// Writes or updates <c>&lt;!-- confluence-page-id: ... --&gt;</c> and
    /// <c>&lt;!-- confluence-space-key: ... --&gt;</c> comments in the Markdown file.
    /// Mirrors Python's <c>_update_markdown</c> in publisher.py.
    /// </summary>
    private async Task WritePageIdToMarkdownAsync(
        string filePath, string pageId, string spaceKey, CancellationToken ct)
    {
        var content = await File.ReadAllTextAsync(filePath, ct);

        // Check if comments already exist — update in place
        var pageIdComment = $"<!-- confluence-page-id: {pageId} -->";
        var spaceKeyComment = $"<!-- confluence-space-key: {spaceKey} -->";

        if (PageIdPattern().IsMatch(content))
        {
            content = PageIdPattern().Replace(content, pageIdComment);
            content = SpaceKeyPattern().Replace(content, spaceKeyComment);
            _logger.Debug("Updated existing page-id/space-key in '{Path}'.", filePath);
        }
        else
        {
            // Insert after YAML frontmatter (if present) or at top
            var insertIndex = 0;
            if (content.StartsWith("---\n", StringComparison.Ordinal) || content.StartsWith("---\r\n", StringComparison.Ordinal))
            {
                var endIndex = content.IndexOf("\n---\n", 4, StringComparison.Ordinal);
                if (endIndex < 0)
                    endIndex = content.IndexOf("\r\n---\r\n", 4, StringComparison.Ordinal);

                if (endIndex >= 0)
                {
                    // Move past the closing --- line
                    insertIndex = content.IndexOf('\n', endIndex + 1) + 1;
                }
            }

            var newLine = content.Contains("\r\n") ? "\r\n" : "\n";
            var comments = pageIdComment + newLine + spaceKeyComment + newLine;
            content = content.Insert(insertIndex, comments);
            _logger.Debug("Inserted page-id/space-key into '{Path}'.", filePath);
        }

        await File.WriteAllTextAsync(filePath, content, ct);
    }

    [GeneratedRegex(@"<!--\s*confluence-page-id:\s*\S+\s*-->")]
    private static partial Regex PageIdPattern();

    [GeneratedRegex(@"<!--\s*confluence-space-key:\s*\S+\s*-->")]
    private static partial Regex SpaceKeyPattern();
}
