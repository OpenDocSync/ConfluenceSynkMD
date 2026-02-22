using System.Diagnostics;
using ConfluentSynkMD.ETL.Core;
using ConfluentSynkMD.Models;
using ConfluentSynkMD.Services;
using Serilog;

namespace ConfluentSynkMD.ETL.Extract;

/// <summary>
/// Extract step: reads Markdown files from the local filesystem and populates
/// <see cref="TranslationBatchContext.ExtractedDocumentNodes"/>.
/// Replaces the former <c>MarkdownExtractor</c> streaming implementation.
/// </summary>
public sealed class MarkdownIngestionStep : IPipelineStep
{
    private readonly HierarchyResolver _hierarchyResolver;
    private readonly ILogger _logger;

    public string StepName => "MarkdownIngestion";

    public MarkdownIngestionStep(HierarchyResolver hierarchyResolver, ILogger logger)
    {
        _hierarchyResolver = hierarchyResolver;
        _logger = logger.ForContext<MarkdownIngestionStep>();
    }

    public Task<PipelineResult> ExecuteAsync(TranslationBatchContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.Information("Extracting Markdown files from '{Path}'.", context.Options.Path);

            var tree = _hierarchyResolver.BuildTree(context.Options.Path);
            _logger.Information("Found {Count} top-level document node(s).", tree.Count);

            var count = 0;
            foreach (var node in FlattenTree(tree))
            {
                ct.ThrowIfCancellationRequested();
                context.ExtractedDocumentNodes.Add(node);
                count++;
                _logger.Debug("Extracted: {Path}", node.RelativePath);
            }

            sw.Stop();

            if (count == 0)
            {
                return Task.FromResult(PipelineResult.Abort(
                    StepName,
                    $"No Markdown files found in '{context.Options.Path}'."));
            }

            return Task.FromResult(PipelineResult.Success(StepName, count, sw.Elapsed));
        }
        catch (DirectoryNotFoundException ex)
        {
            sw.Stop();
            return Task.FromResult(PipelineResult.CriticalError(
                StepName,
                $"Source directory not found: {ex.Message}",
                ex));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Task.FromResult(PipelineResult.CriticalError(
                StepName,
                $"Failed to extract Markdown files: {ex.Message}",
                ex));
        }
    }

    /// <summary>
    /// Flattens the document tree using pre-order traversal (parent before children).
    /// This ensures parent pages are created before their children during upload.
    /// Each child node is annotated with its parent's AbsolutePath for hierarchy tracking.
    /// </summary>
    private static IEnumerable<DocumentNode> FlattenTree(
        IReadOnlyList<DocumentNode> nodes, string? parentSourcePath = null)
    {
        foreach (var node in nodes)
        {
            var annotated = node with { ParentSourcePath = parentSourcePath };
            yield return annotated;
            foreach (var child in FlattenTree(node.Children, annotated.AbsolutePath))
            {
                yield return child;
            }
        }
    }
}
