namespace ConfluentSynkMD.ETL.Core;

/// <summary>
/// Builder for constructing ETL pipelines with a fluent API.
/// Allows adding extractors, transformers, and loaders in a declarative way.
/// All components work with the shared <see cref="TranslationBatchContext"/>.
/// </summary>
public class ETLPipelineBuilder
{
    private readonly List<IPipelineStep> _extractors = new();
    private readonly List<IPipelineStep> _transformers = new();
    private readonly List<IPipelineStep> _loaders = new();

    /// <summary>
    /// Adds an extractor step to the pipeline (Extract phase).
    /// </summary>
    /// <param name="step">Extractor step instance.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public ETLPipelineBuilder AddExtractor(IPipelineStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _extractors.Add(step);
        return this;
    }

    /// <summary>
    /// Adds a transformer step to the pipeline (Transform phase).
    /// </summary>
    /// <param name="step">Transformer step instance.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public ETLPipelineBuilder AddTransformer(IPipelineStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _transformers.Add(step);
        return this;
    }

    /// <summary>
    /// Adds a loader step to the pipeline (Load phase).
    /// </summary>
    /// <param name="step">Loader step instance.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public ETLPipelineBuilder AddLoader(IPipelineStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _loaders.Add(step);
        return this;
    }

    /// <summary>
    /// Builds the ordered list of all pipeline steps: Extractors → Transformers → Loaders.
    /// </summary>
    public IReadOnlyList<IPipelineStep> Build()
    {
        var allSteps = new List<IPipelineStep>(_extractors.Count + _transformers.Count + _loaders.Count);
        allSteps.AddRange(_extractors);
        allSteps.AddRange(_transformers);
        allSteps.AddRange(_loaders);
        return allSteps;
    }

    /// <summary>
    /// Convenience method: builds and immediately executes the pipeline.
    /// </summary>
    public async Task<PipelineResult> ExecuteAsync(
        TranslationBatchContext context,
        PipelineRunner runner,
        CancellationToken ct = default)
    {
        var steps = Build();
        return await runner.RunAsync(steps, context, ct);
    }
}
