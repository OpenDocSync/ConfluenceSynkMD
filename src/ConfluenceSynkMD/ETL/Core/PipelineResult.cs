namespace ConfluenceSynkMD.ETL.Core;

/// <summary>
/// Defines the outcome status of a pipeline step or the overall pipeline execution.
/// </summary>
public enum PipelineResultStatus
{
    /// <summary>Step completed without errors.</summary>
    Success,

    /// <summary>Step completed but with non-critical issues (e.g. some items skipped).</summary>
    Warning,

    /// <summary>Step failed with a critical error – pipeline should abort.</summary>
    CriticalError,

    /// <summary>Step or pipeline was intentionally aborted (e.g. validation failure).</summary>
    Abort
}

/// <summary>
/// Immutable result object returned by each pipeline step and the overall pipeline.
/// Carries status, diagnostics, and metrics for observability.
/// </summary>
public sealed class PipelineResult
{
    public PipelineResultStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? StepName { get; init; }
    public Exception? Exception { get; init; }
    public int ItemsProcessed { get; init; }
    public int ItemsFailed { get; init; }
    public TimeSpan Duration { get; init; }

    /// <summary>Whether the pipeline can continue after this result.</summary>
    public bool CanContinue => Status is PipelineResultStatus.Success or PipelineResultStatus.Warning;

    // ─── Factory methods ────────────────────────────────────────────────────

    /// <summary>Creates a successful result.</summary>
    public static PipelineResult Success(string stepName, int itemsProcessed, TimeSpan duration, string? message = null) =>
        new()
        {
            Status = PipelineResultStatus.Success,
            StepName = stepName,
            ItemsProcessed = itemsProcessed,
            Duration = duration,
            Message = message ?? $"Step '{stepName}' completed successfully ({itemsProcessed} items)."
        };

    /// <summary>Creates a warning result (pipeline continues).</summary>
    public static PipelineResult Warning(string stepName, int itemsProcessed, int itemsFailed, TimeSpan duration, string message) =>
        new()
        {
            Status = PipelineResultStatus.Warning,
            StepName = stepName,
            ItemsProcessed = itemsProcessed,
            ItemsFailed = itemsFailed,
            Duration = duration,
            Message = message
        };

    /// <summary>Creates a critical error result (pipeline aborts).</summary>
    public static PipelineResult CriticalError(string stepName, string message, Exception? exception = null) =>
        new()
        {
            Status = PipelineResultStatus.CriticalError,
            StepName = stepName,
            Message = message,
            Exception = exception
        };

    /// <summary>Creates an abort result with a detailed message.</summary>
    public static PipelineResult Abort(string stepName, string message) =>
        new()
        {
            Status = PipelineResultStatus.Abort,
            StepName = stepName,
            Message = message
        };

    public override string ToString() =>
        $"[{Status}] {StepName}: {Message} (Processed: {ItemsProcessed}, Failed: {ItemsFailed}, Duration: {Duration.TotalMilliseconds:F0}ms)";
}
