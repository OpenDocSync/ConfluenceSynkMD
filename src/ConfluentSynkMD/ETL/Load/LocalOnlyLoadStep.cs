using System.Diagnostics;
using ConfluentSynkMD.ETL.Core;
using ConfluentSynkMD.Models;
using Serilog;

namespace ConfluentSynkMD.ETL.Load;

/// <summary>
/// Saves converted documents as Confluence Storage Format (CSF) files
/// to the local filesystem without making any API calls.
/// Used when --mode LocalExport or --local flag is set.
/// </summary>
public sealed class LocalOnlyLoadStep : IPipelineStep
{
    private readonly ILogger _logger;
    private static readonly char[] AlwaysInvalidFileNameChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    public string StepName => "LocalExport";

    public LocalOnlyLoadStep(ILogger logger)
    {
        _logger = logger.ForContext<LocalOnlyLoadStep>();
    }

    public async Task<PipelineResult> ExecuteAsync(TranslationBatchContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var outputDir = Path.Combine(context.Options.Path, ".confluence-export");
        Directory.CreateDirectory(outputDir);

        var exported = 0;
        var failed = 0;

        foreach (var doc in context.TransformedDocuments)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // Create a sanitized filename from the title
                var safeTitle = SanitizeFileName(doc.Title);
                var outputPath = Path.Combine(outputDir, $"{safeTitle}.csf.html");

                // Write the Confluence Storage Format content
                await File.WriteAllTextAsync(outputPath, doc.Content, ct);
                _logger.Information("Exported: {Title} → {Path}", doc.Title, outputPath);

                // Copy referenced attachments
                if (doc.Attachments.Count > 0)
                {
                    var attachDir = Path.Combine(outputDir, "attachments", safeTitle);
                    Directory.CreateDirectory(attachDir);

                    foreach (var attachment in doc.Attachments)
                    {
                        var targetPath = Path.Combine(attachDir, Path.GetFileName(attachment.FileName));
                        if (File.Exists(attachment.AbsolutePath))
                        {
                            File.Copy(attachment.AbsolutePath, targetPath, overwrite: true);
                        }
                    }
                }

                exported++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.Error(ex, "Failed to export {Title}.", doc.Title);
            }
        }

        sw.Stop();
        _logger.Information("Local export complete: {Count} documents → {Dir}",
            exported, outputDir);

        if (exported == 0 && failed > 0)
        {
            return PipelineResult.CriticalError(StepName,
                $"All {failed} documents failed to export.");
        }

        if (failed > 0)
        {
            return PipelineResult.Warning(StepName, exported, failed, sw.Elapsed,
                $"{exported} exported, {failed} failed.");
        }

        return PipelineResult.Success(StepName, exported, sw.Elapsed);
    }

    private static string SanitizeFileName(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new char[title.Length];
        for (var i = 0; i < title.Length; i++)
        {
            var current = title[i];
            var isInvalid = Array.IndexOf(invalid, current) >= 0
                            || Array.IndexOf(AlwaysInvalidFileNameChars, current) >= 0
                            || char.IsControl(current);

            sanitized[i] = isInvalid ? '_' : current;
        }
        return new string(sanitized);
    }
}
