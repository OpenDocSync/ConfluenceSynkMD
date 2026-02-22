namespace ConfluenceSynkMD.ETL.Core;

/// <summary>
/// Unified interface for all ETL pipeline steps (Extract, Transform, Load).
/// Each step reads from and writes to the shared <see cref="TranslationBatchContext"/>.
/// </summary>
public interface IPipelineStep
{
    /// <summary>Human-readable name for logging and result reporting.</summary>
    string StepName { get; }

    /// <summary>
    /// Executes this pipeline step against the shared context.
    /// Must return a <see cref="PipelineResult"/> indicating success, warning, or failure.
    /// </summary>
    Task<PipelineResult> ExecuteAsync(TranslationBatchContext context, CancellationToken ct = default);
}
