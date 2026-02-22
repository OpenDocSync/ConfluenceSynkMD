using System.Diagnostics;
using System.Globalization;
using Serilog;

namespace ConfluenceSynkMD.ETL.Core;

/// <summary>
/// Executes a sequence of <see cref="IPipelineStep"/> instances against
/// a shared <see cref="TranslationBatchContext"/>. Validates after each step
/// and aborts the pipeline on critical errors.
/// </summary>
public sealed class PipelineRunner
{
    private readonly ILogger _logger;

    public PipelineRunner(ILogger logger)
    {
        _logger = logger.ForContext<PipelineRunner>();
    }

    /// <summary>
    /// Runs all provided steps sequentially against the shared context.
    /// After each step, the result is validated — if a step returns
    /// <see cref="PipelineResultStatus.CriticalError"/> or <see cref="PipelineResultStatus.Abort"/>,
    /// the pipeline is halted immediately.
    /// </summary>
    /// <returns>
    /// The final <see cref="PipelineResult"/> — either the failing step's result
    /// or a summary success result if all steps completed.
    /// </returns>
    public async Task<PipelineResult> RunAsync(
        IReadOnlyList<IPipelineStep> steps,
        TranslationBatchContext context,
        CancellationToken ct = default)
    {
        if (steps.Count == 0)
            return PipelineResult.Abort("Pipeline", "No pipeline steps configured.");

        var pipelineStopwatch = Stopwatch.StartNew();
        var totalItemsProcessed = 0;
        var totalItemsFailed = 0;

        _logger.Information("=================================================");
        _logger.Information("Starting ETL Pipeline Execution ({Count} steps)", steps.Count);
        _logger.Information("=================================================");

        foreach (var step in steps)
        {
            ct.ThrowIfCancellationRequested();

            _logger.Information(">>> Executing step: {StepName}", step.StepName);

            PipelineResult result;
            try
            {
                result = await step.ExecuteAsync(context, ct);
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate cancellation
            }
            catch (Exception ex)
            {
                result = PipelineResult.CriticalError(
                    step.StepName,
                    $"Unhandled exception in step '{step.StepName}': {ex.Message}",
                    ex);
            }

            context.StepResults.Add(result);
            totalItemsProcessed += result.ItemsProcessed;
            totalItemsFailed += result.ItemsFailed;

            _logger.Information("    Step '{StepName}' → {Status} ({Items} processed, {Failed} failed, {Duration}ms)",
                result.StepName, result.Status, result.ItemsProcessed, result.ItemsFailed,
                result.Duration.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture));

            // ─── Post-step validation ───────────────────────────────────────

            if (!result.CanContinue)
            {
                _logger.Error("=================================================");
                _logger.Error("Pipeline ABORTED at step '{StepName}': {Message}",
                    result.StepName, result.Message);
                _logger.Error("=================================================");

                if (result.Exception is not null)
                    _logger.Error(result.Exception, "Exception details:");

                return result;
            }

            if (result.Status == PipelineResultStatus.Warning)
            {
                _logger.Warning("    ⚠ Warning in step '{StepName}': {Message}",
                    result.StepName, result.Message);
            }
        }

        pipelineStopwatch.Stop();

        _logger.Information("=================================================");
        _logger.Information("ETL Pipeline Completed Successfully");
        _logger.Information("  Total items processed: {Processed}", totalItemsProcessed);
        _logger.Information("  Total items failed:    {Failed}", totalItemsFailed);
        _logger.Information("  Total duration:        {Duration}ms",
            pipelineStopwatch.ElapsedMilliseconds);

        _logger.Information(
            "  LinkDiagnostics unresolvedLinkFallbacks={UnresolvedCount} webUiPageIdFallbacks={WebUiFallbackCount}",
            context.UnresolvedLinkFallbackCount,
            context.WebUiPageIdFallbackCount);

        if (context.UnresolvedLinkSamples.Count > 0)
        {
            _logger.Warning("  UnresolvedLinkSamples (max 10):");
            foreach (var sample in context.UnresolvedLinkSamples)
            {
                _logger.Warning("    {Sample}", sample);
            }
        }

        if (context.WebUiPageIdFallbackSamples.Count > 0)
        {
            _logger.Information("  WebUiPageIdFallbackSamples (max 10):");
            foreach (var sample in context.WebUiPageIdFallbackSamples)
            {
                _logger.Information("    {Sample}", sample);
            }
        }

        _logger.Information("=================================================");

        return PipelineResult.Success(
            "Pipeline",
            totalItemsProcessed,
            pipelineStopwatch.Elapsed,
            $"Pipeline completed: {totalItemsProcessed} items processed, {totalItemsFailed} failed, " +
            $"{context.UnresolvedLinkFallbackCount} unresolved link fallback(s), " +
            $"{context.WebUiPageIdFallbackCount} WebUI page-id fallback(s).");
    }
}
